using EnvDTE80;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using System.Runtime.InteropServices;

namespace Foundation.VSPackage
{
    public static class Processes
    {
        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);
        [DllImport("ole32.dll")]
        private static extern void GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        private const int m_MaxVersion = 20;
        private const int m_MinVersion = 10;

        public static DTE2 GetDTE()
        {
            DTE2 dte = null;

            for (int version = m_MaxVersion; version >= m_MinVersion; version--)
            {
                string versionString = string.Format("VisualStudio.DTE.{0}.0", version);

                dte = GetCurrent(versionString);

                if (dte != null)
                {
                    return dte;
                }
            }

            throw new Exception(string.Format("Can not get DTE object tried versions {0} through {1}", m_MaxVersion, m_MinVersion));
        }

        /// <summary>
        /// When multiple instances of Visual Studio are running there also multiple DTE available
        /// The method below takes care of selecting the right DTE for the current process
        /// </summary>
        /// <remarks>
        /// Found this at: http://stackoverflow.com/questions/4724381/get-the-reference-of-the-dte2-object-in-visual-c-sharp-2010/27057854#27057854
        /// </remarks>
        public static DTE2 GetCurrent(string versionString)
        {
            //rot entry for visual studio running under current process.
            string rotEntry = String.Format("!{0}:{1}", versionString, System.Diagnostics.Process.GetCurrentProcess().Id);

            IRunningObjectTable rot;
            GetRunningObjectTable(0, out rot);

            IEnumMoniker enumMoniker;
            rot.EnumRunning(out enumMoniker);
            enumMoniker.Reset();

            uint fetched = uint.MinValue;
            IMoniker[] moniker = new IMoniker[1];

            while (enumMoniker.Next(1, moniker, out fetched) == 0)
            {
                IBindCtx bindCtx;
                CreateBindCtx(0, out bindCtx);
                string displayName;
                moniker[0].GetDisplayName(bindCtx, null, out displayName);

                if (displayName == rotEntry)
                {
                    object comObject;

                    rot.GetObject(moniker[0], out comObject);

                    return (EnvDTE80.DTE2)comObject;
                }
            }

            return null;
        }
    }
}
