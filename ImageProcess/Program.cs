using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcess
{
    class Program
    {
        const string CMD_TRANS_BG = "transbg";

        static void Main(string[] args)
        {
            PrintCmd(null);
            ImageChangeBg imgProc = new ImageChangeBg();
            imgProc.SetMode(ImageChangeBg.ChangeBgMode.Around);

            char[] split = { ' ' };
            while (true)
            {
                Console.Write(">");
                string line = Console.ReadLine();
                string[] _args = line.Split(split, StringSplitOptions.RemoveEmptyEntries);
                if (_args.Length == 0)
                {
                    PrintCmd(null);
                    continue;
                }

                switch (_args[0].ToLower())
                {
                    case CMD_TRANS_BG:
                        if (_args.Length != 7)
                        {
                            PrintCmd(CMD_TRANS_BG);
                            break;
                        }
                        string src = _args[1];
                        string dst = _args[2];
                        int r = int.Parse(_args[3]);
                        int g = int.Parse(_args[4]);
                        int b = int.Parse(_args[5]);
                        int threshold = int.Parse(_args[6]);
                        System.Drawing.Color srcColor = System.Drawing.Color.FromArgb(r, g, b);
                        System.Drawing.Color dstColor = System.Drawing.Color.FromArgb(0, r, g, b);

                        imgProc.SetBgColor(srcColor, dstColor, threshold);
                        imgProc.ChangeBG_Dir(src, dst);
                        break;
                    default:
                        PrintCmd(null);
                        break;
                }
                System.GC.Collect();
            }
        }

        static void PrintCmd(string cmd)
        {
            if (CMD_TRANS_BG.Equals(cmd) || string.IsNullOrEmpty(cmd))
            {
                Console.WriteLine();
                Console.WriteLine(string.Format("{0} <src_dir> <dst_dir> <r> <g> <b> <threshold>", CMD_TRANS_BG));
                Console.WriteLine("  -- <r> <g> <b>: from 0-255");
                Console.WriteLine("  -- <threshold>: from 0-255");
                Console.WriteLine();
            }
        }
    }
}
