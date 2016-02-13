using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Management;

namespace PatcherLib
{
    public class MachineInfo
    {
        public static VideoCardInfo ReadGPUInfo()
        {
            VideoCardInfo cards = new VideoCardInfo();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController");
                Dictionary<string, string> data = new Dictionary<string, string>();

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    GPUInfo gi = new GPUInfo();

                    if (queryObj["Name"] != null)
                    {
                        gi.Name = queryObj["Name"].ToString();
                    }

                    if (queryObj["DriverVersion"] != null)
                    {
                        gi.DriverVersion = queryObj["DriverVersion"].ToString();
                    }

                    if (queryObj["AdapterRAM"] != null)
                    {
                        gi.AdapterRam = Util.ConvertBytesToMegabytes(UInt64.Parse(queryObj["AdapterRAM"].ToString()));
                    }

                    cards.VideoCards.Add(gi);
                }
            }
            catch (Exception e)
            {
            }
            finally
            { }

            return cards;
        }

        public static RamInfo ReadRAMInfo()
        {
            RamInfo ram = new RamInfo();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT BlockSize, StartingAddress, EndingAddress FROM Win32_MemoryDevice");
                Dictionary<string, string> data = new Dictionary<string, string>();
                UInt64 endingAddress = 0;
                UInt64 startingAddress = 0;

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    ram.NumberOfDevice++;
                    if (queryObj["EndingAddress"] != null && queryObj["StartingAddress"] != null)
                    {
                        endingAddress = UInt64.Parse(queryObj["EndingAddress"].ToString());
                        startingAddress = UInt64.Parse(queryObj["StartingAddress"].ToString());

                        float ramInThisStick = Util.ConvertKilobytesToMegabytes(endingAddress - startingAddress);
                        ram.Sizes.Add(ramInThisStick);
                        ram.TotalRAM += ramInThisStick;
                    }
                }
            }
            catch (Exception e)
            {

            }
            finally
            { }

            return ram;
        }

        public static DriveInfo ReadLogicalDiskInfo()
        {
            DriveInfo drives = new DriveInfo();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT DeviceID, Description, FileSystem, FreeSpace, Size FROM Win32_LogicalDisk");
                Dictionary<string, string> data = new Dictionary<string, string>();

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    DiskInfo di = new DiskInfo();

                    if (queryObj["DeviceID"] != null)
                    {
                        di.DeviceID = queryObj["DeviceID"].ToString();
                    }

                    if (queryObj["Description"] != null)
                    {
                        di.DriveType = queryObj["Description"].ToString();
                    }

                    if (queryObj["FileSystem"] != null)
                    {
                        di.FileSystem = queryObj["FileSystem"].ToString();
                    }

                    if (queryObj["FreeSpace"] != null)
                    {
                        di.FreeSpace = Util.ConvertBytesToMegabytes(UInt64.Parse(queryObj["FreeSpace"].ToString()));
                    }

                    if (queryObj["Size"] != null)
                    {
                        di.Size = Util.ConvertBytesToMegabytes(UInt64.Parse(queryObj["Size"].ToString()));
                    }

                    drives.Drives.Add(di);
                }
            }
            catch (Exception e)
            {
                
            }
            finally
            { }

            return drives;
        }

        public static OperatingSystemInfo ReadOperatingSystemInfo()
        {
            OperatingSystemInfo os = new OperatingSystemInfo();

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, OSLanguage, Caption, Version, CSDVersion, OtherTypeDescription, OSArchitecture FROM Win32_OperatingSystem");
                Dictionary<string, string> data = new Dictionary<string, string>();

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (queryObj["Caption"] != null)
                    {
                        os.Name = queryObj["Caption"].ToString();
                    }

                    if (queryObj["Version"] != null)
                    {
                        os.Version = queryObj["Version"].ToString();
                    }

                    if (queryObj["CSDVersion"] != null)
                    {
                        os.ServicePack = queryObj["CSDVersion"].ToString();
                    }

                    if (queryObj["OtherTypeDescription"] != null)
                    {
                        os.AdditionalInfo = queryObj["OtherTypeDescription"].ToString();
                    }

                    if (queryObj["OSArchitecture"] != null)
                    {
                        os.Architecture = queryObj["OSArchitecture"].ToString();
                    }

                    if (queryObj["TotalVisibleMemorySize"] != null)
                    {
                        os.VisibleMemory = Util.ConvertKilobytesToMegabytes(UInt64.Parse(queryObj["TotalVisibleMemorySize"].ToString()));
                    }

                    if (queryObj["OSLanguage"] != null)
                    {
                        os.Language = queryObj["OSLanguage"].ToString();
                    }

                    break;
                }
            }
            catch (Exception e)
            {
                os.Name = "Error getting Operating System info: " + e.Message;
            }
            finally
            { }

            return os;
        }

        public static MotherboardInfo ReadMotherboardInfo()
        {
            MotherboardInfo mb = new MotherboardInfo();

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, Vendor FROM Win32_ComputerSystemProduct");
                Dictionary<string, string> data = new Dictionary<string, string>();

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (queryObj["Name"] != null)
                    {
                        mb.Name = queryObj["Name"] as string;
                    }

                    if (queryObj["Vendor"] != null)
                    {
                        mb.Vendor = queryObj["Vendor"] as string;
                    }
                    break;
                }
            }
            catch (Exception e)
            {
                mb.Name = "Error getting Motherboard info: " + e.Message;
            }
            finally
            { }

            return mb;
        }

        public static CPUInfo ReadCPUInfo()
        {
            CPUInfo cpu = new CPUInfo();

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, MaxClockSpeed, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
                Dictionary<string, string> data = new Dictionary<string, string>();

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (queryObj["Name"] != null)
                    {
                        cpu.Name = queryObj["Name"] as string;
                    }

                    if (queryObj["MaxClockSpeed"] != null)
                    {
                        cpu.MaxClockSpeed = uint.Parse(queryObj["MaxClockSpeed"].ToString());
                    }

                    if (queryObj["NumberOfCores"] != null)
                    {
                        cpu.NumberOfCores = int.Parse(queryObj["NumberOfCores"].ToString());
                    }

                    if (queryObj["NumberOfLogicalProcessors"] != null)
                    {
                        cpu.NumberOfLogicalProcessors = int.Parse(queryObj["NumberOfLogicalProcessors"].ToString());
                    }

                    break;
                }
            }
            catch(Exception e)
            {
                cpu.Name = "Error getting CPU info: " + e.Message;
            }
            finally 
            { }
            
            return cpu;
        }
         
       
    }
}
