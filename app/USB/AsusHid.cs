﻿using HidSharp;
using HidSharp.Reports;
using System.Diagnostics;

namespace GHelper.USB;
public static class AsusHid
{
    public const int ASUS_ID = 0x0b05;

    public const byte INPUT_ID = 0x5a;
    public const byte AURA_ID = 0x5d;

    static int[] deviceIds = { 0x1a30, 0x1854, 0x1869, 0x1866, 0x19b6, 0x1822, 0x1837, 0x1854, 0x184a, 0x183d, 0x8502, 0x1807, 0x17e0, 0x18c6, 0x1abe };

    static HidStream? auraStream;

    public static IEnumerable<HidDevice>? FindDevices(byte reportId)
    {
        HidDeviceLoader loader = new HidDeviceLoader();
        IEnumerable<HidDevice> deviceList;

        try
        {
            deviceList = loader.GetDevices(ASUS_ID).Where(device => deviceIds.Contains(device.ProductID) && device.CanOpen && device.GetMaxFeatureReportLength() > 0);
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"Error enumerating HID devices: {ex.Message}");
            yield break;
        }

        foreach (var device in deviceList)
            if (device.GetReportDescriptor().TryGetReport(ReportType.Feature, reportId, out _))
                yield return device;
    }

    public static HidStream? FindHidStream(byte reportId)
    {
        try
        {
            return FindDevices(reportId)?.FirstOrDefault()?.Open();
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"Error accessing HID device: {ex.Message}");
        }

        return null;
    }

    public static void WriteInput(byte[] data, string log = "USB")
    {
        foreach (var device in FindDevices(INPUT_ID))
        {
            try
            {
                using (var stream = device.Open())
                {
                    var payload = new byte[device.GetMaxFeatureReportLength()];
                    Array.Copy(data, payload, data.Length);
                    stream.SetFeature(payload);
                    Logger.WriteLine($"{log} Feature {device.ProductID.ToString("X")}|{device.GetMaxFeatureReportLength()}: {BitConverter.ToString(data)}");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Error setting feature {device.GetMaxFeatureReportLength()} {device.DevicePath}: {BitConverter.ToString(data)} {ex.Message}");

            }
        }
    }

    public static void Write(byte[] data, string log = "USB")
    {
        Write(new List<byte[]> { data }, log);
    }

    public static void Write(List<byte[]> dataList, string log = "USB")
    {
        var devices = FindDevices(AURA_ID);
        if (devices is null) return;

        try
        {
            foreach (var device in devices)
                using (var stream = device.Open())
                    foreach (var data in dataList)
                    {
                        stream.Write(data);
                        Logger.WriteLine($"{log} {device.ProductID.ToString("X")}: {BitConverter.ToString(data)}");
                    }
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"Error writing {log}: {ex.Message}");
        }

    }

    public static void WriteAura(byte[] data)
    {

        if (auraStream == null) auraStream = FindHidStream(AURA_ID);
        if (auraStream == null) return;

        try
        {
            auraStream.Write(data);
        }
        catch (Exception ex)
        {
            auraStream.Dispose();
            Debug.WriteLine($"Error writing data to HID device: {ex.Message}");
        }
    }

}
