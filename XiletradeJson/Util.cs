using XiletradeJson.Models;
using System.Text;
using CsvHelper.Configuration;
using System.Globalization;
using CsvHelper;

namespace XiletradeJson
{
    internal static class Util
    {
        internal static Bases? BasesEn { get; set; }
        internal static Mods? ModsEn { get; set; }
        internal static Monsters? MonstersEn { get; set; }

        // not used atm, progam run once.
        internal static void ReInitEnglishData() 
        {
            BasesEn = null;
            ModsEn = null;
            MonstersEn = null;
        }

        // Method that create what Xiletrade needs: smallest possible json files. Refactor needed.
        internal static string? CreateJson(string csvRawData, string datName, string jsonPath)
        {
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                };
                
                var reader = new StringReader(csvRawData);
                var csv = new CsvReader(reader, config);

                bool isBases = datName == Strings.BaseItemTypes;
                bool isMods = datName == Strings.Mods;
                bool isMonsters = datName == Strings.MonsterVarieties;
                bool isWords = datName == Strings.Words;

                KeyValuePair<int, string>[]? arrayIndex = isBases ? Strings.BasesIndex
                            : isMods ? Strings.ModsIndex
                            : isMonsters ? Strings.MonstersIndex
                            : isWords ? Strings.WordsIndex : null;

                if (arrayIndex is null)
                {
                    throw new Exception("Header not found for DAT : " + datName);
                }

                List<ResultData> listResultData = new();
                List<WordResultData> listWordResultData = new();

                // PARSING CSV TO JSON
                string[]? header = null;
                while (csv.Read())
                {
                    if(header is null)
                    {
                        csv.ReadHeader();
                        header = csv.HeaderRecord;
                        foreach (var idx in arrayIndex)
                        {
                            if (header?[idx.Key] != idx.Value)
                            {
                                throw new Exception($"Index not found in the header : {header?[idx.Key]} does not equal {idx.Value}");
                            }
                        }
                        continue;
                    }

                    if (isBases)
                    {
                        ResultData d = new()
                        {
                            ID = csv.GetField(0)?.Replace(Strings.Parser.MetaItem.Key, Strings.Parser.MetaItem.Value),
                            Name = csv.GetField(4)?.Replace(Strings.Parser.HexSpaceSring.Key, Strings.Parser.HexSpaceSring.Value), // 0xa0 => 0x20;
                            InheritsFrom = csv.GetField(5)?.Replace(Strings.Parser.MetaItem.Key, Strings.Parser.MetaItem.Value)
                        };

                        if (BasesEn is not null)
                        {
                            var resultDat = BasesEn.Result?[0].Data?.FirstOrDefault(x => x.ID == d.ID);
                            if (resultDat is null)
                            {
                                continue;
                            }
                            d.NameEn = resultDat.Name;
                        }
                        else
                        {
                            d.NameEn = csv.GetField(4);
                        }

                        bool continueLoop = false;

                        if (d.Name.Contains(Strings.Parser.NameBaseUnwanted, StringComparison.Ordinal))
                        {
                            continueLoop = true;
                        }
                        if (d.InheritsFrom == Strings.Parser.StackableCurrency && 
                            !d.ID.Contains(Strings.Parser.IncursionVial, StringComparison.Ordinal))
                        {
                            continue;
                        }
                        foreach (var str in Strings.Parser.InheritsBaseUnwanted)
                        {
                            if (d.InheritsFrom == str)
                            {
                                continueLoop = true;
                                break;
                            }
                        }
                        if (continueLoop) continue;

                        if (listResultData.FirstOrDefault(x => x.Name == d.Name) == null) listResultData.Add(d);
                    }
                    if (isMods)
                    {
                        ResultData d = new()
                        {
                            ID = csv.GetField(0),
                            InheritsFrom = Strings.Parser.ModsInherits
                        };
                        
                        bool checkId = false;
                        foreach (var id in Strings.Parser.IdModsUnwanted)
                        {
                            if (d.ID?.IndexOf(id, StringComparison.Ordinal) == 0)
                            {
                                checkId = true;
                                break;
                            }
                        }
                        if (checkId)
                        {
                            continue;
                        }

                        StringBuilder sb = new(csv.GetField(9));
                        if (sb.Length == 0)
                        {
                            continue;
                        }
                        foreach (var kvp in Strings.Parser.ModNameFix)
                        {
                            sb.Replace(kvp.Key, kvp.Value);
                        }
                        d.Name = sb.ToString();

                        if (ModsEn is not null)
                        {
                            var resultDat = ModsEn.Result?[0].Data?.FirstOrDefault(x => x.ID == d.ID);
                            if (resultDat is null)
                            {
                                continue;
                            }
                            d.NameEn = resultDat.Name;
                        }
                        else
                        {
                            d.NameEn = sb.ToString();
                        }

                        if (listResultData.FirstOrDefault(x => x.Name == d.Name) == null) listResultData.Add(d);
                    }
                    if (isMonsters)
                    {
                        ResultData d = new()
                        {
                            ID = csv.GetField(0)?.Replace(Strings.Parser.MetaMonster.Key, Strings.Parser.MetaMonster.Value),
                            Name = csv.GetField(32),
                            InheritsFrom = csv.GetField(8)?.Replace(Strings.Parser.MetaMonster.Key, Strings.Parser.MetaMonster.Value)
                        };

                        if (MonstersEn is not null)
                        {
                            var resultDat = MonstersEn.Result?[0].Data?.FirstOrDefault(x => x.ID == d.ID);
                            if (resultDat is null)
                            {
                                continue;
                            }
                            d.NameEn = resultDat.Name;
                        }
                        else
                        {
                            d.NameEn = csv.GetField(32);
                        }
                        if (listResultData.FirstOrDefault(x => x.Name == d.Name) == null) listResultData.Add(d);
                    }
                    if (isWords)
                    {
                        WordResultData d = new()
                        {
                            Name = csv.GetField(5)?.Trim(),
                            NameEn = csv.GetField(1)?.Trim()
                        };
                        
                        StringBuilder sb = new(d.Name);
                        foreach (var kvp in Strings.Parser.WordNameFix)
                        {
                            sb.Replace(kvp.Key, kvp.Value);
                        }
                        string[] tempName = sb.ToString().Split('/');

                        for (int k = 0; k < tempName.Length; k++)
                        {
                            if (d.Name == null)
                            {
                                d.Name = tempName[k].Trim();
                            }
                            else if (!d.Name.Contains(tempName[k].Trim(), StringComparison.Ordinal))
                            {
                                d.Name = d.Name + '/' + tempName[k].Trim();
                            }
                        }
                        if (listWordResultData.FirstOrDefault(x => x.Name == d.Name) == null) listWordResultData.Add(d);
                    }
                }
                // END OF PARSING
                if (listWordResultData.Count > 0)
                {
                    return WriteJson(datName, jsonPath, listWordResultData);
                }
                return WriteJson(datName, jsonPath, listResultData);
            }
            catch (Exception e)
            {
                throw;
                //string mess = e.Message;
            }
        }

        internal static string? WriteJson(string datName, string jsonPath, List<ResultData> listResultData)
        {
            string? outputJson = null;

            if (listResultData.Count == 0)
            {
                return null;
            }

            if (datName == Strings.BaseItemTypes)
            {
                Bases bases = new();
                bases.Result = new Result[1];
                bases.Result[0] = new();
                bases.Result[0].Data = new ResultData[listResultData.Count];
                bases.Result[0].Data = listResultData.ToArray();

                if (BasesEn is null)
                {
                    BasesEn = bases;
                }

                outputJson = jsonPath + "Bases.json";
                using (StreamWriter writer = new(outputJson, false, Encoding.UTF8))
                {
                    writer.Write(Json.Serialize<Bases>(bases));
                }
            }
            if (datName == Strings.Mods)
            {
                Mods mods = new();
                mods.Result = new Result[1];
                mods.Result[0] = new();
                mods.Result[0].Data = new ResultData[listResultData.Count];
                mods.Result[0].Data = listResultData.ToArray();

                if (ModsEn is null)
                {
                    ModsEn = mods;
                }

                outputJson = jsonPath + datName + ".json";
                using (StreamWriter writer = new(outputJson, false, Encoding.UTF8))
                {
                    writer.Write(Json.Serialize<Mods>(mods));
                }
            }
            if (datName == Strings.MonsterVarieties)
            {
                Monsters monsters = new();
                monsters.Result = new Result[1];
                monsters.Result[0] = new();
                monsters.Result[0].Data = new ResultData[listResultData.Count];
                monsters.Result[0].Data = listResultData.ToArray();

                if (MonstersEn is null)
                {
                    MonstersEn = monsters;
                }

                outputJson = jsonPath + "Monsters.json";
                using (StreamWriter writer = new(outputJson, false, Encoding.UTF8))
                {
                    writer.Write(Json.Serialize<Monsters>(monsters));
                }
            }
            return outputJson;
        }

        internal static string? WriteJson(string datName, string jsonPath, List<WordResultData> listWordResultData)
        {
            string? outputJson = null;

            if (datName == Strings.Words && listWordResultData.Count > 0)
            {
                Words words = new();
                words.Result = new WordResult[1];
                words.Result[0] = new();
                words.Result[0].Data = new WordResultData[listWordResultData.Count];
                words.Result[0].Data = listWordResultData.ToArray();

                outputJson = jsonPath + datName + ".json";
                using (StreamWriter writer = new(outputJson, false, Encoding.UTF8))
                {
                    writer.Write(Json.Serialize<Words>(words));
                }
            }
            return outputJson;
        }
    }
}
