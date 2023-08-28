using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.LLVMInterop
{
    public static class LLVMOptimizer
    {
        private const string optPath = @"C:\Users\colton\source\repos\cxx-common-cmake-win\cxx-common-cmake\build\install\bin\opt.exe";

        public static LLVMModuleRef Optimize(LLVMModuleRef module, string llPath)
        {
            module.PrintToFile(llPath);
            var dir = Path.GetDirectoryName(llPath);
            var fileName = Path.GetFileName(llPath);
            var newPath = Path.Combine(dir, Path.ChangeExtension(fileName, ".opt.ll"));

            // Optimize the module.
            RunProcess(optPath, @$"-passes=instcombine,aggressive-instcombine,instcombine,gvn,gvn,newgvn,instcombine,aggressive-instcombine,instcombine,aggressive-instcombine,instcombine,gvn,gvn,newgvn,instcombine,aggressive-instcombine,instcombine,aggressive-instcombine,instcombine,gvn,gvn,newgvn,instcombine,aggressive-instcombine,reassociate,aggressive-instcombine,indvars,sccp,adce,gvn,gvn,reassociate,gvn,reassociate,gvn,reassociate,gvn,reassociate,gvn,reassociate,gvn,newgvn,reassociate,gvn,newgvn,reassociate,gvn,newgvn,reassociate,gvn,newgvn,reassociate,gvn,newgvn,reassociate,gvn,newgvn,reassociate,gvn,newgvn,reassociate,gvn,newgvn,reassociate,gvn,newgvn,reassociate,gvn,newgvn,instcombine,gvn,newgvn,instcombine,reassociate -memdep-block-number-limit=10000000 -dse-memoryssa-defs-per-block-limit=10000000 -gvn-max-num-deps=25000000 -dse-memoryssa-scanlimit=900000000 -dse-memoryssa-partial-store-limit=90000000 -memdep-block-scan-limit=1000000000 -enable-store-refinement=1 -memssa-check-limit=99999999 -gvn-max-num-visited-insts=99999999 -dse-memoryssa-walklimit=99999999 -dse-memoryssa-partial-store-limit=9999999  -dse-memoryssa-path-check-limit=9999999 -instcombine-max-sink-users=9999999 -S ""{llPath}"" -o {newPath}");

            // Return a new module with the optimized IR.
            return module.Context.ParseIR(CreateMemoryBuffer(Path.Combine(Directory.GetCurrentDirectory(), newPath)));
        }

        private static void RunProcess(string exePath, string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.Start();

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception("command failed.");
            }
        }

        public static unsafe LLVMMemoryBufferRef CreateMemoryBuffer(string filePath)
        {
            LLVMMemoryBufferRef handle;
            sbyte* msg;
            if (LLVM.CreateMemoryBufferWithContentsOfFile(new MarshaledString(filePath), (LLVMOpaqueMemoryBuffer**)&handle, &msg) != 0)
                throw new InvalidOperationException();

            return handle;
        }
    }
}
