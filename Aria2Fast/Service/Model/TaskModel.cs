﻿using Aria2NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Aria2Fast.Service.Model
{


    public class TaskModel : BaseNotificationModel
    {
        //获取展示用的名字

        public DownloadStatusResult Data { set; get; }


        public bool FromSubscription
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Data.Gid) &&
                    SubscriptionManager.Instance.TaskUrlToSubscriptionName.ContainsKey(Data.Gid))
                {
                    return true;
                }
                if (!string.IsNullOrWhiteSpace(Data.InfoHash) &&
                    SubscriptionManager.Instance.TaskUrlToSubscriptionName.Any(a => a.Key.Contains(Data.InfoHash)))
                {
                    return true;
                }
                return false;
            }

        }

        /// <summary>
        /// 绑定的是此代码来显示名字
        /// </summary>
        public string ShowName
        {
            get
            {
                if (Data != null)
                {
                    if (Data.Bittorrent != null)
                    {
                        var name = Data.Bittorrent.Info?.Name!;
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            name = Data.InfoHash + ".torrent";
                        }
                        return name;
                    }
                    else if (Data.Files != null && Data.Files.Count > 0)
                    {
                        var uri = Data?.Files?[0]?.Uris?.FirstOrDefault()?.Uri;
                        return Path.GetFileName(uri);
                    }
                    return Data.Gid;
                }

                return "";//SubscriptionManager.Instance.TaskUrlToSubscriptionName.ContainsKey(Data.Url);
            }
        }

        public string SubscriptionName
        {
            get
            {
                if (FromSubscription)
                {
                    SubscriptionManager.Instance.TaskUrlToSubscriptionName.TryGetValue(Data.Gid, out var name);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name;
                    }

                    SubscriptionManager.Instance.TaskUrlToSubscriptionName.TryGetValue(Data.InfoHash!, out string nameFromHash);
                    if (!string.IsNullOrWhiteSpace(nameFromHash))
                    {
                        return nameFromHash;
                    }

                    var kv = SubscriptionManager.Instance.TaskUrlToSubscriptionName.LastOrDefault(a => a.Key.Contains(Data.InfoHash));
                    if (!string.IsNullOrWhiteSpace(kv.Value))
                    {
                        return kv.Value;
                    }


                    return ShowName;

                }
                else
                {
                    return ShowName;
                }
            }
        }



        /// <summary>
        /// 根据错误的类型来获取错误信息
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                if (Data != null)
                {
                    string message = Data.ErrorCode switch
                    {
                        "0" => "", //没有问题 所有下载成功
                        "1" => "未知错误",
                        "2" => "超时",
                        "3" => "资源未找到",
                        "4" => "aria2发现指定数量的'资源未找到'错误。参见 --max-file-not-found 选项",
                        "5" => "由于下载速度太慢而中止下载。参见 --lowest-speed-limit 选项",
                        "6" => "网络错误",
                        "7" => "有未完成的下载。只有在所有已完成的下载都成功，并且在用户按 Ctrl-C 或发送 TERM 或 INT 信号退出 aria2 时队列中仍有未完成的下载，才会报告此错误",
                        "8" => "远程服务器在需要完成下载的情况下不支持恢复",
                        "9" => "磁盘空间不足",
                        "10" => "块长度与 .aria2 控制文件中的不一致。参见 --allow-piece-length-change 选项",
                        "11" => "aria2正在下载相同的文件",
                        "12" => "aria2正在下载相同信息散列的BT种子",
                        "13" => "文件已存在",
                        "14" => "重命名文件失败",
                        "15" => "aria2无法打开现有文件",
                        "16" => "aria2无法创建新文件或截断现有文件",
                        "17" => "文件I/O错误",
                        "18" => "aria2无法创建目录",
                        "19" => "名称解析失败",
                        "20" => "无法解析 Metalink 文档",
                        "21" => "FTP命令失败",
                        "22" => "HTTP响应头不正确或不符合预期",
                        "23" => "重定向过多",
                        "24" => "HTTP授权失败",
                        "25" => "无法解析 bencoded 文件（通常是 “.torrent” 文件）",
                        "26" => "“.torrent” 文件被损坏或缺少 aria2 需要的信息",
                        "27" => "Magnet URI 损坏",
                        "28" => "给定了错误的或无法识别的选项，或给定了意外的选项",
                        "29" => "由于暂时过载或维护，远程服务器无法处理请求",
                        "30" => "无法解析 JSON-RPC 请求",
                        "31" => "预留。未使用",
                        "32" => "效验和验证失败",
                        _ => ""
                    };
                    return message;
                }

                return "";
            }
        }

        public string ErrorMessageMin
        {
            get
            {
                if (Data != null)
                {
                    string message = Data.ErrorCode switch
                    {
                        "0" => "", //没有问题 所有下载成功
                        "1" => "未知错误",
                        "2" => "超时",
                        "3" => "资源未找到",
                        "4" => "资源未找到",
                        "5" => "下载速度慢中止下载",
                        "6" => "网络错误",
                        "7" => "有未完成的下载",
                        "8" => "远程服务器在需要完成下载的情况下不支持恢复",
                        "9" => "磁盘空间不足",
                        "10" => "块长度与控制文件不一致",
                        "11" => "存在相同的文件",
                        "12" => "存在相同Hash的种子",
                        "13" => "文件已存在",
                        "14" => "重命名文件失败",
                        "15" => "无法打开现有文件",
                        "16" => "无法创建新文件或打开现有文件",
                        "17" => "文件I/O错误",
                        "18" => "无法创建目录",
                        "19" => "名称解析失败",
                        "20" => "无法解析Metalink",
                        "21" => "FTP命令失败",
                        "22" => "HTTP响应不正确",
                        "23" => "重定向过多",
                        "24" => "HTTP授权失败",
                        "25" => "无法解析BT文件",
                        "26" => "BT文件被损坏",
                        "27" => "MagnetURI不正确",
                        "28" => "未知错误#2", //"给定了错误的或无法识别的选项，或给定了意外的选项",
                        "29" => "远程服务器无法处理请求", //"由于暂时过载或维护，远程服务器无法处理请求",
                        "30" => "无法解析JSON-RPC请求",
                        "31" => "预留未使用",
                        "32" => "效验和验证失败",
                        _ => ""//没有问题
                    };
                    return message;
                }

                return "";
            }
        }

        public string Link
        {
            get
            {
                if (Data != null && Data.Bittorrent != null)
                {
                    var link = $"magnet:?xt=urn:btih:{Data.InfoHash}";
                    return link;
                }
                else
                {
                    try
                    {
                        var link = Data!.Files.First().Uris.First().Uri.ToString();
                        return link;
                    }
                    catch (Exception ex)
                    {

                    }
                }

                return "";
            }
        }
    }
}
