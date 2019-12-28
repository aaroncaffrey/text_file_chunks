using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace text_file_chunks
{
    public class Program
    {
        public static List<List<T>> split_sequence<T>(List<T> seq, int sections, int divisible = 2, bool distribute = true)
        {
            var r = new List<List<T>>();
            for (var i = 0; i < sections; i++)
            {
                r.Add(new List<T>());
            }

            if (seq == null || seq.Count == 0)
            {
                return r;
            }

            var len = seq.Count;

            if (sections > len) sections = len;

            var div = len / sections;
            var mod = len % sections;
            var mod_start_index = ((sections / 2) - (mod / 2));
            var mod_end_index = mod_start_index + (mod - 1);
            var centre = (sections / 2);
            var taken = 0;
            for (var i = 0; i < sections; i++)
            {
                if (taken >= len) break;
                var take = div + (distribute && i >= mod_start_index && i <= mod_end_index ? 1 : 0) + (!distribute && i == centre ? mod : 0);

                if (divisible != 0)
                {
                    var rem = take % divisible;

                    take += rem;

                    //while (take % divisible != 0)
                    //{
                    //    take++;
                    //}
                }

                var x = seq.Skip(taken).Take(take).ToList();

                r[i].AddRange(x);
                taken += x.Count;
            }

            return r;
        }

        public enum split_number
        {
            line_count,
            line_count_half,
            processor_count,
            processor_count_double
        }

        public static void Main(string[] args)
        {

            //var s = "12345678901234567";
            //var x= split_sequence(s.ToList(), 3, 0,false);
            //foreach (var a in x)
            //{
            //    Console.WriteLine(string.Join("",a));
            //}

            //Console.ReadLine();
            //return;



            //var files = new List<string>()
            //{
            //    @"c:\blast\psiblast_nr_3_local.bat",
            //    @"c:\blast\psiblast_nr_remote.bat",
            //    @"c:\blast\psiblast_swissprot_remote.bat",
            //    @"c:\blast\psiblast_swissprot_3_local.bat",
            //};

            //var root = @"C:\foldx\";
            //var file_prefix = "_foldx";
            //var cmd_name = "_foldx"; // foldx

            var root = @"C:\betastrands_dataset\dna\";
            var file_prefix = "_dna";
            var cmd_name = "_dna"; // foldx


            var split_type = split_number.processor_count;

            var distribute = true;

            var files = Directory.GetFiles(root, "*.bat", SearchOption.TopDirectoryOnly);            

            var callers = new List<string>();
            var callers2 = new List<string>();

            var first = true;

            var chunk_finish_cmds = new List<string>() { /*"pause", */"exit" };

            foreach (var file in files)
            {
                Console.WriteLine("Processing: " + file);
                var input = File.ReadAllLines(file).Where(a=>!string.IsNullOrWhiteSpace(a)).ToList();

                var sections = 0;

                switch (split_type)
                {
                    case split_number.line_count:
                        sections = input.Count;
                        break;

                    case split_number.line_count_half:
                        sections = input.Count / 2;
                        break;
                    case split_number.processor_count:
                        sections = Environment.ProcessorCount;
                        break;
                    case split_number.processor_count_double:
                        sections = Environment.ProcessorCount * 2;

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var split = Program.split_sequence(input, sections, 0, distribute);

                split = split.Where(a => a.Count(b => !string.IsNullOrWhiteSpace(b)) > 0).ToList(); // new code, untested

                if (split.Sum(a => a.Count) != input.Count) throw new Exception();

                var total_chucks = split.Count;

                var pad_amount = total_chucks.ToString().Length;


                for (var index = 0; index < split.Count; index++)
                {
                    var chunk = split[index];

                    chunk.Reverse();

                    var chunk_empty = chunk.Count(a => !string.IsNullOrWhiteSpace(a)) == 0;

                    if (chunk_empty) continue;
                    //for (var i = 0; i < chunk.Count; i++)
                    //{
                    //    var line = chunk[i];

                    //    var pssm_file = line.Split('=').Last();
                    //    chunk.Insert(i+1,$@"copy ""{pssm_file}"" ""{pssm_file.Replace(@"c:\",@"h:\")}""");
                    //    i++;
                    //}

                    var output_file = Path.Combine(Path.GetDirectoryName(file), $"{file_prefix}{Path.GetFileNameWithoutExtension(file)}_{(index + 1).ToString().PadLeft(pad_amount, '0')}_of_{total_chucks}{Path.GetExtension(file)}");
                    var caller_file2 = Path.Combine(Path.GetDirectoryName(file), $"{file_prefix}{Path.GetFileNameWithoutExtension(file)}_caller{Path.GetExtension(file)}");
                    var caller_file = Path.Combine(Path.GetDirectoryName(file),  $"{file_prefix}{cmd_name}_caller_{(index + 1).ToString().PadLeft(pad_amount, '0')}_of_{total_chucks}" + Path.GetExtension(file));

                    if (!callers.Contains(caller_file)) callers.Add(caller_file);
                    if (!callers2.Contains(caller_file2)) callers2.Add(caller_file2);

                    File.WriteAllLines(output_file, chunk);
                    File.AppendAllLines(output_file, chunk_finish_cmds);

                    File.AppendAllLines(caller_file,new [] { "START " + output_file });
                    File.AppendAllLines(caller_file2, new [] { "START " + output_file });
                    // note: use one caller file - not both - that would run the batch files twice

                    //File.AppendAllLines(caller_file,new [] { "CALL " + output_file });
                    Console.WriteLine("Saving: " + output_file);
                }

                Console.WriteLine();
            }

            var caller_finish_cmds = new List<string>() {"exit"};

            foreach (var caller in callers)
            {
                File.AppendAllLines(caller, caller_finish_cmds);
            }

            foreach (var caller in callers2)
            {
                File.AppendAllLines(caller, caller_finish_cmds);
            }

            var callers_caller_file = Path.Combine(Path.GetDirectoryName(root), $"{file_prefix}{cmd_name}_caller.bat");

            File.WriteAllLines(callers_caller_file, callers.Select(a=>"START " + a).ToList());
            File.AppendAllLines(callers_caller_file, caller_finish_cmds);

            Console.WriteLine("Finished.");
            Console.ReadLine();
        }
    }
}
