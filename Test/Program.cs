﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using NewLife.Common;
using NewLife.CommonEntity;
using NewLife.Compression;
using NewLife.IO;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Xml;
using XCode.Cache;
using XCode.DataAccessLayer;
using XCode.Sync;
using XCode.Transform;
using NewLife;
using NewLife.Net.IO;
using NewLife.Net.Stun;

namespace Test
{
    public class Program
    {
        private static void Main(string[] args)
        {
            XTrace.UseConsole();
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                Test15();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void Test2()
        {
            HttpClientMessageProvider client = new HttpClientMessageProvider();
            client.Uri = new Uri("http://localhost:8/Web/MessageHandler.ashx");

            var rm = MethodMessage.Create("Admin.Login", "admin", "admin");
            //rm.Header.Channel = 88;

            //Message.Debug = true;
            //var ms = rm.GetStream();
            //var m2 = Message.Read(ms);

            Message msg = client.SendAndReceive(rm, 0);
            var rs = msg as EntityMessage;
            Console.WriteLine("返回：" + rs.Value);

            msg = client.SendAndReceive(rm, 0);
            rs = msg as EntityMessage;
            Console.WriteLine("返回：" + rs.Value);
        }

        static void Test3()
        {
            var uri = new NetUri("udp://x2:3389");

            Console.WriteLine(uri);
            Console.WriteLine(uri.ProtocolType);
            Console.WriteLine(uri.EndPoint);
            Console.WriteLine(uri.Address);
            Console.WriteLine(uri.Host);
            Console.WriteLine(uri.Port);

            var xml = uri.ToXml();
            Console.WriteLine(xml);

            uri = xml.ToXmlEntity<NetUri>();
            Console.WriteLine(uri);
        }

        static void Test5()
        {
            using (var zf = new ZipFile())
            {
                zf.AddFile("XCode.pdb");
                zf.AddFile("NewLife.Core.pdb");

                zf.Write("test.zip");
                zf.Write("test.7z");
            }
            //using (var zf = new ZipFile("Test.lzma.zip"))
            //{
            //    foreach (var item in zf.Entries.Values)
            //    {
            //        Console.WriteLine("{0} {1}", item.FileName, item.CompressionMethod);
            //    }

            //    zf.Extract("lzma");
            //}
        }

        static void Test6()
        {
            Message.DumpStreamWhenError = true;
            //var msg = new EntityMessage();
            //msg.Value = Guid.NewGuid();
            var msg = new MethodMessage();
            msg.TypeName = "Admin";
            msg.Name = "Login";
            msg.Parameters = new Object[] { "admin", "password" };

            var kind = RWKinds.Json;
            var ms = msg.GetStream(kind);
            //ms = new MemoryStream(ms.ReadBytes(ms.Length - 1));
            //Console.WriteLine(ms.ReadBytes().ToHex());
            Console.WriteLine(ms.ToStr());
            //ms = msg.GetStream(RWKinds.Xml);
            //Console.WriteLine(ms.ToStr());

            Message.Debug = true;
            ms.Position = 0;
            var rs = Message.Read(ms, kind);
            Console.WriteLine(rs);
        }

        static void Test7()
        {
            //Console.Write("请输入表达式：");
            //var code = Console.ReadLine();

            //var rs = ScriptEngine.Execute(code, new Dictionary<String, Object> { { "a", 222 }, { "b", 333 } });
            ////Console.WriteLine(rs);

            //var se = ScriptEngine.Create(code);
            //var fm = code.Replace("a", "{0}").Replace("b", "{1}");
            //for (int i = 1; i <= 9; i++)
            //{
            //    for (int j = 1; j <= i; j++)
            //    {
            //        Console.Write(fm + "={2}\t", j, i, se.Invoke(i, j));
            //    }
            //    Console.WriteLine();
            //}

            var se = ScriptEngine.Create("Test.Program.TestMath(k)");
            if (se.Method == null)
            {
                se.Parameters.Add("k", typeof(Double));
                se.Compile();
            }

            var fun = (DM)(Object)Delegate.CreateDelegate(typeof(DM), se.Method as MethodInfo);

            var timer = 1000000;
            var k = 123;
            CodeTimer.ShowHeader();
            CodeTimer.TimeLine("原生", timer, n => TestMath(k));
            CodeTimer.TimeLine("动态", timer, n => se.Invoke(k));
            CodeTimer.TimeLine("动态2", timer, n => fun(k));
        }
        public static Double TestMath(Double k)
        {
            //var bts = File.ReadAllBytes(Assembly.GetExecutingAssembly().Location);
            return Math.Sin(k) * Math.Log10(k) * Math.Exp(k);
        }
        delegate Object DM(Double k);

        static SysConfig Load()
        {
            var filename = SysConfig._.ConfigFile;
            if (filename.IsNullOrWhiteSpace()) return null;
            filename = filename.GetFullPath();
            if (!File.Exists(filename)) return null;

            try
            {
                var config = filename.ToXmlFileEntity<SysConfig>();
                if (config == null) return null;

                //config.OnLoaded();

                //// 第一次加载，建立定时重载定时器
                //if (timer == null && _.ReloadTime > 0) timer = new TimerX(s => Current = null, null, _.ReloadTime * 1000, _.ReloadTime * 1000);

                return config;
            }
            catch (Exception ex) { XTrace.WriteException(ex); return null; }
        }

        static void Test9()
        {
            var tb = Administrator.Meta.Table.DataTable;
            var table = ObjectContainer.Current.Resolve<IDataTable>();
            table = table.CopyAllFrom(tb);

            // 添加两个字段
            var fi = table.CreateColumn();
            fi.ColumnName = "LastUpdate";
            fi.DataType = typeof(DateTime);
            table.Columns.Add(fi);

            fi = table.CreateColumn();
            fi.ColumnName = "LastSync";
            fi.DataType = typeof(DateTime);
            table.Columns.Add(fi);

            var dal = DAL.Create("Common99");
            // 检查架构
            dal.SetTables(table);

            var sl = new SyncSlave();
            sl.Factory = dal.CreateOperate(table.TableName);

            var mt = new SyncMaster();
            mt.Facotry = Administrator.Meta.Factory;
            //mt.LastUpdateName = Administrator._.LastLogin;

            var sm = new SyncManager();
            sm.Slave = sl;
            sm.Master = mt;

            sm.Start();
        }

        static void Test11()
        {
            var str = "78-01-6B-CC-D1-64-68-08-2B-67-00-01-21-86-2D-9F-4F-AD-3F-BB-FD-F8-EB-3D-6F-0D-8C-0C-4C-F5-8A-CB-D3-18-0F-2F-60-30-04-CB-32-30-70-33-94-A7-E6-24-E7-E7-A6-EA-95-54-94-00-00-38-FD-12-D4";
            var buf = str.ToHex().ReadBytes(2);
            Console.WriteLine("压缩原文：");
            Console.WriteLine(buf.ToHex("-", 16));
            Console.WriteLine("Length={0}", buf.Length);
            var old = buf;

            buf = buf.Decompress();
            var old2 = buf;
            Console.WriteLine("解压：");
            Console.WriteLine(buf.ToHex("-", 16));
            Console.WriteLine("Length={0}", buf.Length);

            buf = buf.Compress();
            Console.WriteLine("压缩：");
            Console.WriteLine(buf.ToHex("-", 16));
            Console.WriteLine("Length={0}", buf.Length);
            Console.WriteLine(buf.CompareTo(old));

            buf = buf.Decompress();
            Console.WriteLine("再次解压：");
            Console.WriteLine(buf.ToHex("-", 16));
            Console.WriteLine("Length={0}", buf.Length);
            Console.WriteLine(buf.CompareTo(old2));

            str = "81-6C-29-00-80-56-77-00-00-00-00-00-12-00-B4-F3-CA-AF-CD-B7-C7-EB-BC-ED-30-32-30-35-2E-73-77-66-01-C3-A0-00-31-00-00-00-00-00-00-00-0B-00-77-65-6C-63-6F-6D-65-2E-74-78-74";
            old = buf = str.ToHex();
            str = "78-01-ED-BD-07-60-1C-49-96-25-26-2F-6D-CA-7B-7F-4A-F5-4A-D7-E0-74-A1-08-80-60-13-24-D8-90-40-10-EC-C1-88-CD-E6-92-EC-1D-69-47-23-29-AB-2A-81-CA-65-56-65-5D-66-16-40-CC-ED-9D-BC-F7-DE-7B-EF-BD-F7-DE-7B-EF-BD-F7-BA-3B-9D-4E-27-F7-DF-FF-3F-5C-66-64-01-6C-F6-CE-4A-DA-C9-9E-21-80-AA-C8-1F-3F-7E-7C-1F-3F-22-FE-E0-F2-CE-AF-F1-07-FD-E4-D5-AF-81-E7-B7-F8-35-FE-B6-5F-F6-CF-FC-8D-FF-FC-DF-F9-4F-FE-B7-7F-DF-7F-BF-B3-B7-73-7F-DC-5C-9D-FF-9A-FF-E8-5F-F4-6B-EC-FE-1A-F2-FC-86-BF-C6-55-5E-4E-AB-45-3E-6E-DF-B5-FF-0F";
            var buf2 = str.ToHex().ReadBytes(2);

            Console.WriteLine("原文：");
            Console.WriteLine(buf.ToHex("-", 16));
            buf = buf.Compress();
            Console.WriteLine("压缩后：");
            Console.WriteLine(buf.ToHex("-", 16));
            Console.WriteLine("Length={0}", buf.Length);
            Console.WriteLine(buf.CompareTo(buf2));

            buf = buf.Decompress();
            Console.WriteLine("解压后：");
            Console.WriteLine(buf.ToHex("-", 16));
            Console.WriteLine("Length={0}", buf.Length);
            Console.WriteLine(buf.CompareTo(old));
        }

        //static NetServer test12Server = null;
        //static Thread[] test12Ths = null;
        //static void Test12()
        //{
        //    test12Server = new NetServer();
        //    test12Server.Address = IPAddress.Parse("192.168.2.55");
        //    test12Server.Port = 9000;
        //    test12Server.ProtocolType = System.Net.Sockets.ProtocolType.Tcp;
        //    test12Server.AddressFamily = AddressFamily.InterNetwork;
        //    test12Server.NewSession += new EventHandler<NetEventArgs>(test12Server_Accepted);
        //    test12Server.Received += new EventHandler<NetEventArgs>(test12Server_Received);
        //    test12Server.Start();
        //    foreach (var item in test12Server.Servers)
        //    {
        //        if (item is TcpServer)
        //        {
        //            //(item as TcpServer).MaxNotActive = 5;
        //            (item as TcpServer).ShowEventLog = true;
        //        }
        //    }

        //    //test12Ths = new Thread[1];
        //    //for (int i = 0; i < test12Ths.Length; i++)
        //    //{
        //    //    test12Ths[i] = new Thread(test12Send);
        //    //    test12Ths[i].Name = "thread " + i.ToString();
        //    //    test12Ths[i].IsBackground = true;
        //    //    test12Ths[i].Start();
        //    //}

        //}

        //static void test12Server_Accepted(object sender, NetEventArgs e)
        //{
        //    Console.WriteLine("accept client");
        //}

        //static void test12Server_Received(object sender, NetEventArgs e)
        //{
        //    //e.Session.Send(e.GetStream());
        //    Console.WriteLine(e.GetString());

        //    // 以下代码不会执行
        //    // 内部通过OnError已处理，
        //    if (e.BytesTransferred == 0)
        //    {
        //        ////if (e.SocketError == SocketError.OperationAborted || e.SocketError == SocketError.ConnectionReset)
        //        ////{

        //        ////    return;
        //        ////}

        //        if (e.SocketError != SocketError.Success || e.Error != null)
        //            Console.WriteLine("{0} {1}错误 {2} {3}", sender, e.LastOperation, e.SocketError, e.Error);
        //        else
        //            Console.WriteLine("{0} {1}断开！", sender, e.LastOperation);
        //    }
        //}
        //static void test12Send()
        //{
        //    ISocketClient session = NetService.CreateSession(new NetUri("Tcp://127.0.0.1:9000"));
        //    return;
        //    //服务端检测不到有客户端连接
        //    Thread.Sleep(3000);
        //    //while (true)
        //    {
        //        Thread.Sleep(10000);
        //        try
        //        {
        //            session.Send("abcdefghijklmn");
        //            //此时同时触发客户端连接事件和接收数据事件。
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message);
        //        }
        //    }
        //}

        static void Test8()
        {
            //// 删除数据库
            //var file = "Common.db";
            //if (File.Exists(file)) File.Delete(file);

            XTrace.Debug = false;
            // 关闭SQL日志，关闭线程池调试
            DAL.ShowSQL = false;
            DAL.Debug = false;
            CacheSetting.Debug = false;
            ThreadPoolX.Debug = false;

            TestSQLite(100, 0);
            TestSQLite(100, 1);
            TestSQLite(100, 2);
            TestSQLite(100, 5);
            TestSQLite(100, 10);

            TestSQLite(50, 0);
            TestSQLite(50, 1);
            TestSQLite(50, 2);
            TestSQLite(50, 5);
            TestSQLite(50, 10);

            TestSQLite(20, 0);
            TestSQLite(20, 1);
            TestSQLite(20, 2);
            TestSQLite(20, 5);
            TestSQLite(20, 10);

            TestSQLite(10, 0);
            TestSQLite(10, 1);
            TestSQLite(10, 2);
            TestSQLite(10, 5);
            TestSQLite(10, 10);

            TestSQLite(5, 0);
            TestSQLite(5, 1);
            TestSQLite(5, 2);
            TestSQLite(5, 5);
            TestSQLite(5, 10);
        }

        static void TestSQLite(Int32 maxths = 50, Int32 timeout = 0)
        {
            var key = String.Format("Common_{0}_{1}", maxths, timeout);
            var file = key + ".db";
            if (File.Exists(file)) File.Delete(file);

            if (!DAL.ConnStrs.ContainsKey(key)) DAL.AddConnStr(key, "Data Source=" + file + ";Default Timeout=" + timeout, null, "SQLite");
            // 修改默认链接，所有线程生效
            Administrator.Meta.Table.ConnName = key;
            Administrator.Meta.ConnName = key;

            // 查询预热一次
            var admin = Administrator.FindByID(1);
            if (admin != null) admin.Delete();

            //Thread.Sleep(2000);
            Console.Write("{0,3}并发{1,2}s延迟 ", maxths, timeout);

            // 使用线程池
            var pool = ThreadPoolX.Instance;
            // 最大工作线程
            pool.MaxThreads = maxths;

            idx = 0;
            Total = 0;
            Error = 0;

            var sw = new Stopwatch();
            sw.Start();
            // 任务数量为最大工作线程的10倍
            var count = pool.MaxThreads * 10;
            for (int i = 0; i < count; i++) pool.Queue(Test8_2, key);

            pool.WaitAll(200);
            var max = count * 20.0;
            var left = Console.CursorLeft;
            while (true)
            {
                Console.CursorLeft = left;

                Console.Write("正确：{0} 错误：{1} 完成：{2:p} 速度：{3}", Total, Error, (Total + Error) / max, (Int32)(Total / sw.Elapsed.TotalSeconds));

                if (pool.RunningCount <= 0 && pool.QueryCount() <= 0) break;
                Thread.Sleep(500);
            }
            //Console.WriteLine();
            sw.Stop();
            Console.WriteLine(" 耗时 {0}s", (Int32)sw.Elapsed.TotalSeconds);

            if (File.Exists(file))
            {
                DAL.Create(key).Db.Dispose();
                GC.Collect();
                Thread.Sleep(1000);
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        static Int32 idx = 0;
        static Int32 Total = 0;
        static Int32 Error = 0;

        static void Test8_2(Object state)
        {
            Administrator.Meta.ConnName = state + "";

            var tid = Thread.CurrentThread.ManagedThreadId;
            var rnd = new Random((Int32)DateTime.Now.Ticks);
            var pre = "Test_" + tid + "_" + rnd.Next(999999999) + "_";
            using (var trans = Administrator.Meta.CreateTrans())
            {
                // 每个线程重复10次插入操作
                for (int i = 0; i < 20; i++)
                {
                    //Thread.Sleep(100);
                    // 全局计数，避免名称重复
                    Interlocked.Increment(ref idx);
                    try
                    {
                        var admin = new Administrator();
                        admin.Name = pre + idx;
                        admin.RoleID = 1;
                        admin.Insert();

                        Interlocked.Increment(ref Total);
                    }
                    catch (Exception ex)
                    {
                        // 输出非数据库锁定错误
                        if (!ex.Message.Contains(" is locked"))
                            XTrace.WriteLine(ex.Message);

                        Interlocked.Increment(ref Error);
                    }
                }

                trans.Commit();
            }
        }

        static void Test10()
        {
            // 扫描所有文件样本，检测编码
            var root = @"E:\Auto\STM32F1\";
            foreach (var fi in Directory.GetFiles(root, "*.c", SearchOption.AllDirectories))
            {
                Console.WriteLine(fi.TrimStart(root));

                var encoding = fi.AsFile().ReadBytes(0, 4).DetectBOM();
                if (encoding != null)
                {
                    Console.WriteLine("固定编码：{0}", encoding.EncodingName);
                }
                else
                {
                    var encoding1 = EncodingHelper.Detect(fi);
                    var encoding2 = Encoding.UTF8;
                    // 特殊打开，为了获取文件编码
                    using (var reader = new StreamReader(fi, encoding2, true))
                    {
                        encoding2 = reader.CurrentEncoding;
                        var txt = reader.ReadToEnd();
                    }

                    Console.WriteLine("结果：{0}\t{1} vs {2}", encoding1 == encoding2,
                        encoding1 == null ? "" : encoding1.EncodingName, encoding2.EncodingName);
                }
            }
        }

        static void Test13()
        {
            var file = @"E:\BaiduYunDownload\xiaomi.db";
            var file2 = Path.ChangeExtension(file, "sqlite");
            DAL.AddConnStr("src", "Data Source=" + file, null, "sqlite");
            DAL.AddConnStr("des", "Data Source=" + file2, null, "sqlite");

            if (!File.Exists(file2))
            {
                var et = new EntityTransform();
                et.SrcConn = "src";
                et.DesConn = "des";
                //et.PartialTableNames.Add("xiaomi");
                //et.PartialCount = 1000000;

                et.Transform();
            }

            var sw = new Stopwatch();

            var dal = DAL.Create("src");
            var eop = dal.CreateOperate(dal.Tables[0].TableName);
            sw.Start();
            var count = eop.Count;
            sw.Stop();
            XTrace.WriteLine("{0} 耗时 {1}ms", count, sw.ElapsedMilliseconds);
            sw.Reset(); sw.Start();
            count = eop.FindCount();
            sw.Stop();
            XTrace.WriteLine("{0} 耗时 {1}ms", count, sw.ElapsedMilliseconds);

            var entity = eop.Create();
            entity["username"] = "Stone";
            entity.Save();
            count = eop.FindCount();
            Console.WriteLine(count);

            entity.Delete();
            count = eop.FindCount();
            Console.WriteLine(count);
        }

        static void Test14()
        {
            Console.Clear();
            //XTrace.Log.Level = LogLevel.Info;
            var server = new FileServer();
            server.Log = XTrace.Log;
            server.Start();

            var count = 0;
            WaitCallback func = s =>
            {
                count++;
                var client = new FileClient();
                try
                {
                    client.Log = XTrace.Log;
                    if (s + "" == "Test.exe")
                        client.Connect("127.0.0.1", server.Port);
                    else
                        client.Connect("::1", server.Port);
                    client.SendFile(s + "");
                }
                finally
                {
                    count--;
                    client.Dispose();
                }
            };

            ThreadPoolX.QueueUserWorkItem(func, "Test.exe");
            ThreadPoolX.QueueUserWorkItem(func, "NewLife.Core.dll");
            ThreadPoolX.QueueUserWorkItem(func, "NewLife.Net.dll");

            var file = @"F:\MS\cn_visual_studio_ultimate_2013_with_update_4_x86_dvd_5935081.iso";
            if (File.Exists(file))
                ThreadPoolX.QueueUserWorkItem(func, file);

            Thread.Sleep(500);
            while (count > 0) Thread.Sleep(200);
            server.Dispose();
        }

        static void Test15()
        {
            //NetHelper.Debug = true;
            //var server = new StunServer();
            //server.Start();

            var client = new StunClient();
            //var pb = client.GetPublic();
            //Console.WriteLine(pb);
            var rs = client.Query();
            if (rs != null)
            {
                //if (rs != null && rs.Type == StunNetType.Blocked && rs.Public != null) rs.Type = StunNetType.Symmetric;
                XTrace.WriteLine("网络类型：{0} {1}", rs.Type, rs.Type.GetDescription());
                XTrace.WriteLine("公网地址：{0}", rs.Public);
            }
        }
    }
}