using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace TtlCalcSim;

public static class Program
{
    public static void Main(string[] args)
    {
        var alu74181 = new Alu74181();
        var alu = new Alu(alu74181);
        var inputOutput = new InputOutput();
        var sim = new TtlCalcSim(alu, inputOutput);

        using (var fs = File.OpenRead(args[0]))
        {
            sim.LoadProg(fs);
        }

        while (true)
        {
            sim.RunStep();
        }
    }
}