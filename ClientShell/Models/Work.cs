using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Drawing;

namespace ClientShell
{
    public class WorkInfo
    {
        public string Name { get; }
        public Bitmap Icon { get; }
        public int Cost { get; }
        public string Description { get; }
        public byte[] AssemblyBytes { get; }

        private Assembly assembly;
        public void Run()
        {
            assembly??= Assembly.Load(AssemblyBytes);

            assembly.EntryPoint.Invoke(null, null);
        }

    }
}
