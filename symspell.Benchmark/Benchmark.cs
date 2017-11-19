﻿using System;
using System.Diagnostics;
using System.IO;

namespace symspell.Benchmark
{
    class Benchmark
    {
        static readonly string Path = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string Query1k = Path+"../../../symspelldemo/test_data/noisy_query_en_1000.txt";

        static readonly string[] DictionaryPath = {
            Path+"../../../symspelldemo/test_data/frequency_dictionary_en_30_000.txt",
            Path+"../../../symspell/frequency_dictionary_en_82_765.txt",
            Path+"../../../symspelldemo/test_data/frequency_dictionary_en_500_000.txt" };

        static readonly string[] DictionaryName = {
            "30k",
            "82k",
            "500k" };

        static string[] BuildQuery1K()
        {
            string[] testList = new string[1000];
            //load 1000 terms with random spelling errors
            int i = 0;
            using (StreamReader sr = new StreamReader(File.OpenRead(Query1k)))
            {
                String line;

                //process a single line at a time only for memory efficiency
                while ((line = sr.ReadLine()) != null)
                {
                    string[] lineParts = line.Split(null);
                    if (lineParts.Length >= 2)
                    {
                        testList[i++] = lineParts[0];
                    }
                }
            }
            return testList;
        }

        static void Main(string[] args)
        {
            Console.BufferHeight = 10000;

            BenchPrecalculation();

            Console.WriteLine();
            Console.Write("complete, press any key...");
            Console.ReadKey();
        }

        static void BenchPrecalculation()
        {
            string[] query1k = BuildQuery1K();
            int resultNumber = 0;
            int repetitions = 1000;

            Stopwatch stopWatch = new Stopwatch();
            for (int maxEditDistance=1; maxEditDistance <=3; maxEditDistance++)
            {
                for (int prefixLength = 5; prefixLength <= 7; prefixLength++)
                {
                    SymSpell dict = new SymSpell(maxEditDistance, prefixLength);
                    Original.SymSpell dictOrig = new Original.SymSpell(maxEditDistance, prefixLength);

                    //benchmark dictionary precalculation size and time 
                    //maxEditDistance=1/2/3; prefixLength=5/6/7;  dictionary=30k/82k/500k; class=instantiated/static
                    for (int i=0;i< DictionaryPath.Length;i++)
                    {
                        //instantiated dictionary                      
                        long memSize = GC.GetTotalMemory(true);
                        stopWatch.Restart();
                        dict.LoadDictionary(DictionaryPath[i], 0, 1);
                        stopWatch.Stop();
                        long memDelta = GC.GetTotalMemory(true) - memSize;
                        Console.WriteLine("Precalculation instance "+stopWatch.Elapsed.TotalSeconds.ToString("N1")+"s "+(memDelta/1024/1024).ToString("N0")+ "MB " +" MaxEditDistance=" + maxEditDistance.ToString() + " prefixLength=" + prefixLength.ToString() + " dict=" + DictionaryName[i]);

                        //static dictionary 
                        memSize = GC.GetTotalMemory(true);
                        stopWatch.Restart();
                        dictOrig.LoadDictionary(DictionaryPath[i], "", 0, 1);
                        stopWatch.Stop();
                        memDelta = GC.GetTotalMemory(true) - memSize;
                        Console.WriteLine("Precalculation static   " + stopWatch.Elapsed.TotalSeconds.ToString("N1") + "s " + (memDelta / 1024 / 1024).ToString("N0") + "MB " + " MaxEditDistance=" + maxEditDistance.ToString() + " prefixLength=" + prefixLength.ToString() + " dict=" + DictionaryName[i]);

                        //benchmark lookup result number and time
                        //maxEditDistance=1/2/3; prefixLength=5/6/7; dictionary=30k/82k/500k; verbose=0/1/2; query=exact/non-exact/mix; class=instantiated/static
                        for (int verbose=0; verbose <= 2; verbose++)
                        {
                            //instantiated exact
                            stopWatch.Restart();
                            for (int round=0;round<repetitions;round++) resultNumber=dict.Lookup("different", maxEditDistance, verbose).Count;
                            stopWatch.Stop();             
                            Console.WriteLine("Lookup instance "+resultNumber.ToString("N0") + " results " + ((double)stopWatch.ElapsedMilliseconds/(double)repetitions).ToString("N3") + "ms/op verbose=" + verbose.ToString() + " query=exact");
                            //static exact
                            stopWatch.Restart();
                            for (int round = 0; round < repetitions; round++) resultNumber = dictOrig.Lookup("different", "", maxEditDistance, verbose).Count;
                            stopWatch.Stop();
                            Console.WriteLine("Lookup static   " + resultNumber.ToString("N0") + " results " + ((double)stopWatch.ElapsedMilliseconds / (double)repetitions).ToString("N3") + "ms/op verbose=" + verbose.ToString() + " query=exact");
                            Console.WriteLine();

                            //instantiated non-exact
                            stopWatch.Restart();
                            for (int round = 0; round < repetitions; round++) resultNumber = dict.Lookup("hockie", maxEditDistance, verbose).Count;
                            stopWatch.Stop();
                            Console.WriteLine("Lookup instance " + resultNumber.ToString("N0") + " results " + ((double)stopWatch.ElapsedMilliseconds / (double)repetitions).ToString("N3") + "ms/op verbose=" + verbose.ToString() + " query=non-exact");
                            //static non-exact
                            stopWatch.Restart();
                            for (int round = 0; round < repetitions; round++) resultNumber = dictOrig.Lookup("hockie", "", maxEditDistance, verbose).Count;
                            stopWatch.Stop();
                            Console.WriteLine("Lookup static   "+resultNumber.ToString("N0") + " results " + ((double)stopWatch.ElapsedMilliseconds / (double)repetitions).ToString("N3") + "ms/op verbose=" + verbose.ToString() + " query=non-exact");
                            Console.WriteLine();

                            //instantiated mix                           
                            stopWatch.Restart();
                            resultNumber = 0; foreach (var word in query1k) resultNumber+=dict.Lookup(word, maxEditDistance, verbose).Count;
                            stopWatch.Stop();
                            Console.WriteLine("Lookup instance " + resultNumber.ToString("N0") + " results " + ((double)stopWatch.ElapsedMilliseconds/(double)query1k.Length).ToString("N3") + "ms/op verbose=" + verbose.ToString() + " query=mix");
                            //static mix                           
                            stopWatch.Restart();
                            resultNumber = 0; foreach (var word in query1k) resultNumber += dictOrig.Lookup(word, "", maxEditDistance, verbose).Count;
                            stopWatch.Stop();
                            Console.WriteLine("Lookup static   " + resultNumber.ToString("N0") + " results " + ((double)stopWatch.ElapsedMilliseconds / (double)query1k.Length).ToString("N3") + "ms/op verbose=" + verbose.ToString() + " query=mix");
                            Console.WriteLine();
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
