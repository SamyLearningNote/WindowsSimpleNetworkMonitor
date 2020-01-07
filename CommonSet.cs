﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// import the library of windows network
using System.Net.NetworkInformation;
using System.Diagnostics;

// import the library for auto start
using Microsoft.Win32;
using System.Diagnostics;

class CommonSet
{
    // veriable set
    public NetworkInterface[] interfaces;

    public double previousUploadValue = 0;
    public double previousDownloadValue = 0;

    public double uploadTrafficValue;
    public double downloadTrafficValue;

    public string configFileName = "configuration";
    public string floatingPositionFileName = "position";
    public string configurationExecutableName = "SNTMConfiguration.exe";
    public string floatingWindowExecutableName = "SNTMFloatingWindow.exe";
    public string baseProgramExecutableName = "SNTMStartProcess.exe";
    public string booterProgramExecutableName = "SNTMBooter.exe";

    public string configurationProcessName = "SNTMConfiguration";
    public string floatingWindowProcessName = "SNTMFloatingWindow";
    public string baseProgramProcessName = "SNTMStartProcess";
    public string booterProgramProcessName = "SNTMBooter";

    public int loadedLanguageIndex = 0;
    public int loadedInterfaceIndex = 0;
    public double loadedUpdateFrequency = 1;
    public int loadedSpeedUnitIndex = 0;
    public string loadedSpeedUnit = "";
    public int loadedDisplayMethodIndex = 0;
    public int loadedDisplaySizeIndex = 0;
    public int loadedDarkThemeIndex = 0;
    public bool loadedAutoStartCheck = true;
    public bool loadedShowBubbleCheck = false;

    public double maxFrequency = 10;
    public double minFrequency = 0.1;

    public int numberOfSpeedUnit = 6;
    public int numberOfWindowSize = 3;
    public int numberOfLanguage = 2;

    public long previousSystemTime = 0;
    public long currentSystemTime = 0;

    public bool CheckIfRunning(String programName)
    {
        Process[] processes = Process.GetProcessesByName(programName);
        // check if there any program running with same name
        if (processes.Length > 0)
        {
            return true;
        }
        return false;
    }

    public bool CheckIfRunningSameProcess(String programName)
    {
        Process[] processes = Process.GetProcessesByName(programName);
        // check if there any program running with same name
        if (processes.Length > 1)
        {
            return true;
        }
        return false;
    }

    public bool CheckIfNameChanged(String programName)
    {
        Process[] processes = Process.GetProcessesByName(programName);
        // check if there any program running with same name
        if (processes.Length < 1)
        {
            return true;
        }
        return false;
    }

    public void ApplyAutoStartSetting(bool autoStartChecked)
    {
        RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        // enable or disable if the auto start check box is checked or not
        if (autoStartChecked)
        {
            rkApp.SetValue("SNTMauto", System.AppDomain.CurrentDomain.BaseDirectory + booterProgramExecutableName);
        }
        else
        {
            rkApp.DeleteValue("SNTMauto", false);
        }

    }

    public bool SaveFloatingWindowPosition(double xPos, double yPos)
    {
        try
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(System.AppDomain.CurrentDomain.BaseDirectory + floatingPositionFileName, false))
            {
                // update the position
                file.WriteLine(String.Format("{0:F2}", xPos));
                file.WriteLine(String.Format("{0:F2}", yPos));
            }
        }
        catch
        {
            return false;
        }
        return true;
    }

    public void InitCommonSet()
    {
        GetInterfaceInformation();
        // get the time when the software is started
        GetCurrentTimeToPreviousTime();
    }

    private void GetCurrentTimeToPreviousTime()
    {
        this.previousSystemTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    private void GetInterfaceInformation()
    {
        // get network interface information

        // check if the network is available
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            return;
        }

        // get the list of network interface
        interfaces = NetworkInterface.GetAllNetworkInterfaces();
    }

    // check if this computer have any network interface, need to run init common set (get interface information) first
    public bool CheckIfHaveInterface()
    {
        if (interfaces.Length > 0)
        {
            return true;
        }
        return false;
    }

    // start dpecified process
    public bool StartProcess(string targetExecutable)
    {
        try
        {
            
            System.Diagnostics.Process.Start(System.AppDomain.CurrentDomain.BaseDirectory + targetExecutable);
        }
        catch
        {
            return false;
        }
        return true;
    }

    // end specified process
    public void EndProcess(string targetedProcess)
    {
        // check if targeted process is opened, if yes close all the floating window
        Process[] processes = Process.GetProcessesByName(targetedProcess);
        foreach (var process in processes)
        {
            process.Kill();
        }

    }

    // end all process
    public void EndAllProcess()
    {
        // close all the related application
        // check if floating window is opened, if yes close all the floating window
        Process[] processes = Process.GetProcessesByName(floatingWindowProcessName);
        foreach (var process in processes)
        {
            process.Kill();
        }

        // check if configuration window is opened, if yes close all the floating window
        processes = Process.GetProcessesByName(configurationProcessName);
        foreach (var process in processes)
        {
            process.Kill();
        }

        // close booter
        processes = Process.GetProcessesByName(booterProgramProcessName);
        foreach (var process in processes)
        {
            process.Kill();
        }

        // close this application
        processes = Process.GetProcessesByName(baseProgramProcessName);
        foreach (var process in processes)
        {
            process.Kill();
        }


    }

    public void GetTrafficInformation(int SpeedUnitSelectedIndex, int selectedInterfaceIndex)
    {
        /*Task.Factory.StartNew(() =>
        {*/
        double divisionParameter = 1.0;
        long uploadTrafficValueTemp = interfaces[selectedInterfaceIndex].GetIPv4Statistics().BytesSent;
        long downloadTrafficValueTemp = interfaces[selectedInterfaceIndex].GetIPv4Statistics().BytesReceived;

        // get the value corrseponding to the selected speed
        uploadTrafficValue = uploadTrafficValueTemp - previousUploadValue;
        downloadTrafficValue = downloadTrafficValueTemp - previousDownloadValue;

        // calculate the division parameter
        for (int i = 0; i <= SpeedUnitSelectedIndex; i++)
        {
            if (i % 2 == 0)
            {
                divisionParameter *= 1024;
            }
        }

        // if the unit is for small "b", then * 8
        if (SpeedUnitSelectedIndex % 2 == 0)
        {
            divisionParameter /= 8;
        }

        // Get current system time
        currentSystemTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();

        // divide the value accroading to the update frequency
        divisionParameter *= (Convert.ToDouble(currentSystemTime) - Convert.ToDouble(previousSystemTime)) / 1000.0;

        // move current time as previous time for next traffic calculation
        previousSystemTime = currentSystemTime;

        // compute the traffic
        uploadTrafficValue /= divisionParameter;
        downloadTrafficValue /= divisionParameter;

        // update the current value as previous value
        previousUploadValue = uploadTrafficValueTemp;
        previousDownloadValue = downloadTrafficValueTemp;
        //});

    }


    public bool LoadSetting()
    {
        try
        {
            using (System.IO.StreamReader file = new System.IO.StreamReader(System.AppDomain.CurrentDomain.BaseDirectory + configFileName))
            {
                bool formatError = false;
                // read language index
                if (!Int32.TryParse(file.ReadLine(), out loadedLanguageIndex))
                {
                    formatError = true;
                }
                else
                {
                    // check if the language index is in range
                    if (loadedLanguageIndex < 0 || loadedLanguageIndex > numberOfLanguage)
                    {
                        formatError = true;
                    }
                }

                // read interface index
                if (!Int32.TryParse(file.ReadLine(), out loadedInterfaceIndex))
                {
                    formatError = true;
                }
                else
                {
                    // check if the index is in range
                    if (loadedInterfaceIndex < 0 || loadedInterfaceIndex >= interfaces.Length)
                    {
                        formatError = true;
                    }
                }

                // read update frequency
                if (!Double.TryParse(file.ReadLine(), out loadedUpdateFrequency))
                {
                    formatError = true;
                }
                // check if the update frequency is in range
                else if (loadedUpdateFrequency < minFrequency || loadedUpdateFrequency > maxFrequency)
                {
                    formatError = true;
                }

                // read speed unit index
                if (!Int32.TryParse(file.ReadLine(), out loadedSpeedUnitIndex))
                {
                    formatError = true;
                }
                else if (loadedSpeedUnitIndex < 0 || loadedSpeedUnitIndex > numberOfSpeedUnit)
                {
                    formatError = true;
                }

                // read speed unit
                loadedSpeedUnit = file.ReadLine();

                // read show method index
                if (!Int32.TryParse(file.ReadLine(), out loadedDisplayMethodIndex))
                {
                    formatError = true;
                }
                else if (loadedDisplayMethodIndex < 0 || loadedDisplayMethodIndex > 3)
                {
                    formatError = true;
                }

                // read floating window size index
                if (!Int32.TryParse(file.ReadLine(), out loadedDisplaySizeIndex))
                {
                    formatError = true;
                }
                else if (loadedDisplaySizeIndex < 0 || loadedDisplaySizeIndex >= numberOfWindowSize)
                {
                    formatError = true;
                }

                // read dark mode index
                if (!Int32.TryParse(file.ReadLine(), out loadedDarkThemeIndex))
                {
                    formatError = true;
                }
                else
                {
                    // check if the dark mode index is in range
                    if (loadedDarkThemeIndex < 0 || loadedDarkThemeIndex > 1)
                    {
                        formatError = true;
                    }
                }

                // read auto start option
                if (!Boolean.TryParse(file.ReadLine(), out loadedAutoStartCheck))
                {
                    formatError = true;
                }

                // read show bubble option
                if (!Boolean.TryParse(file.ReadLine(), out loadedShowBubbleCheck))
                {
                    formatError = true;
                }

                // check if the format of file correct
                if (formatError)
                {
                    // format of the file is wrong, return false
                    return false;
                }
                else
                {
                    // return true to tell the file readed successfully
                    return true;
                }

            }
        }
        catch
        {
            // error occur, return false
            return false;
        }
    }
}
