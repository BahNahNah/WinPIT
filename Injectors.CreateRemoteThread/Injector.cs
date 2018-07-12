﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Engine;
using Engine.Extensions;
using Engine.Injectors;
using Engine.ProcessCore;
using Module = Engine.ProcessCore.Module;

namespace Injectors.CreateRemoteThread
{
    public class Injector : IInjector
    {
        private Logger log = new Logger(LoggerType.Console_File, "Injector.CRT");

        public string SelfFileName
        {
            get { return Path.GetFileName(Assembly.GetExecutingAssembly().Location); }
        }

        public string UniqueId
        {
            get
            {
                return "Injectors.CRT-" + QuickExt.GetHash(
                           Encoding.UTF8.GetBytes(UniqueName +
                                                  Marshal.GetTypeLibGuidForAssembly(Assembly.GetExecutingAssembly())
                                                      .ToString()), HashType.MD5);
            }
        }

        public string UniqueName
        {
            get { return "Injection by CreateRemoteThread API"; }
        }

        public string About
        {
            get
            {
                return
                    "API: CreateRemoteThread" + Environment.NewLine +
                    "DLL: kernel32.dll" + Environment.NewLine + Environment.NewLine +
                    "Stealth: None" + Environment.NewLine +
                    "Kernel/System/Normal Access: Normal" + Environment.NewLine +
                    "Original Author: Unknown - existed for years!"
                    ;
            }
        }

        public Module InjectedModule { get; set; }
        public IntPtr Inject(Core targetProcess, string filePath)
        {
            //Logger.StartLogger(Environment.UserInteractive ? LoggerType.Console : LoggerType.File, "Injector.CRT");

            InjectedModule = new Module(filePath);

            var loadLib = targetProcess.GetLoadLibraryPtr();
            if (loadLib == IntPtr.Zero)
            {
                log.Log(LogType.Error, "Cannot retrieve LoadLibraryA pointer - aborting!");
                return IntPtr.Zero;
            }
            var pathBytes = Encoding.Unicode.GetBytes(filePath);

            var alloc = targetProcess.Allocate(pathBytes.Length);
            if (alloc == IntPtr.Zero)
            {
                log.Log(LogType.Error, "Cannot allocate memory in attached process - aborting!");
                return IntPtr.Zero;
            }

            if (!targetProcess.WriteBytes(pathBytes, alloc))
            {
                log.Log(LogType.Error, "Cannot write file-path to memory - aborting!");
                return IntPtr.Zero;
            }

            var crt = targetProcess.CreateThread(loadLib, alloc);
            if (crt == IntPtr.Zero)
                log.Log(LogType.Failure, "Failed on creating thread inside attached process: {0}",
                    Marshal.GetLastWin32Error().ToString("X"));
            else
                log.Log(LogType.Success, "Thread created succesfully inside attached process: 0x{0}",
                    (Environment.Is64BitProcess ? crt.ToInt64().ToString("X") : crt.ToInt32().ToString("X")));

            return crt;
        }
    }
}
