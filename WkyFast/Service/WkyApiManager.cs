﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WkyApiSharp.Service;
using WkyApiSharp.Service.Model.GetUsbInfo;
using WkyFast.Utils;

namespace WkyFast.Service
{
    public class WkyApiManager
    {
        private static WkyApiManager instance = new WkyApiManager();

        public static WkyApiManager Instance
        {
            get
            {
                return instance;
            }
        }


        public WkyApi WkyApi { set; get; } 

        public ObservableCollection<WkyApiSharp.Service.Model.ListPeer.ResultClass> PeerList { set; get; } = new();

        public WkyApiSharp.Service.Model.ListPeer.Device NowDevice { set; get; }

        public ObservableCollection<WkyApiSharp.Service.Model.ListPeer.Device> DeviceList { set; get; } = new();

        public WkyApiGetUsbInfoResultModel NowUsbInfo { set; get; }

        /// <summary>
        /// 玩客云登录成功后的信息处理
        /// </summary>
        public async Task<int> UpdateDevice()
        {
            var wkyApi = WkyApi;
            //获取设备信息
            var listPeerResult = await wkyApi.ListPeer();
            PeerList.Clear();
            foreach (var item in listPeerResult.Result)
            {
                if (item.ResultClass != null)
                {
                    PeerList.Add(item.ResultClass);
                }
            }

            DeviceList.Clear();
            foreach (var peer in PeerList)
            {
                foreach (var device in peer.Devices)
                {
                    DeviceList.Add(device);
                }
            }
            return DeviceList.Count;
        }

        /// <summary>
        /// 选中设备，优先从上次选择中选中
        /// </summary>
        public async Task<WkyApiSharp.Service.Model.ListPeer.Device?> SelectDevice()
        {
            if (PeerList != null && PeerList.Count > 0)
            {
                WkyApiSharp.Service.Model.ListPeer.Device? selectDevice = null;

                foreach (var peer in PeerList)
                {
                    foreach (var device in peer.Devices)
                    {
                        if (!string.IsNullOrWhiteSpace(AppConfig.ConfigData.LastDeviceId))
                        {
                            if (device.DeviceId == AppConfig.ConfigData.LastDeviceId)
                            {
                                selectDevice = device;
                            }
                        }
                    }
                }


                if (selectDevice != null)
                {
                    //更新USB信息
                    NowUsbInfo = await WkyApi.GetUsbInfo(selectDevice.DeviceId);
                    NowDevice = selectDevice;
                    return selectDevice;
                }

                //到这里说明没有配置AppConfig.ConfigData.LastDeviceId 或者 peer.Devices为空

                selectDevice = PeerList?.First()?.Devices?.First();
                if (selectDevice != null)
                {
                    NowUsbInfo = await WkyApi.GetUsbInfo(selectDevice.DeviceId);
                    NowDevice = selectDevice;
                }
                
                return selectDevice;
            }
            return null;
        }

        public string GetUsbInfoDefPath()
        {
            var savePath = string.Empty;
            if (NowUsbInfo != null && NowUsbInfo.Rtn == 0)
            {
                foreach (var disk in NowUsbInfo.Result)
                {
                    if (disk.ResultClass != null)
                    {
                        foreach (var partition in disk.ResultClass.Partitions)
                        {
                            savePath = partition.Path + "/onecloud/tddownload";
                        }
                    }

                }
            }
            return savePath;
        }
    }
}