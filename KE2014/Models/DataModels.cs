using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Diagnostics;
using System.Configuration;

namespace KE2014.Models
{
    public class Frequency : IComparable
    {
        public int TermFrequency { get; set; }
        public int DocumentFrequency { get; set; }
        public double TFIDF { get; set; }
        public int LastSeen { get; set; }

        public Frequency(int tf, int df, int ls)
        {
            TermFrequency = tf;
            DocumentFrequency = df;
            LastSeen = ls;
        }

        public int CompareTo(object o)
        {
            Frequency other = (Frequency)o;
            if (this.TFIDF == other.TFIDF)
                return 0;
            else if (this.TFIDF < other.TFIDF)
                return -1;
            else
                return 1;
        }
    }

    public class DataContext
    {
        public const int ENTERTAINMENT = 1; // 影劇娛樂
        public const int SPORTS = 2; // 運動
        public const int MAINLAND = 3; // 兩岸
        public const int FINANCE = 4; // 財經
        public const int HEALTH = 5; //保健
        public const int POLITICS = 6; // 政治
        public const int SOCIETY = 7; // 社會
        public static int DocumentCount; // 文件數
        //public static string QueryTime; // 查詢時間

        /// <summary>
        /// 取得類別詞項排名辭典。
        /// </summary>
        /// <remarks>
        /// dictionary[0]: 2-gram
        /// dictionary[1]: 3-gram
        /// dictionary[2]: 4-gram
        /// dictionary[3]: 5-gram
        /// dictionary[4]: 6-gram
        /// dictionary[5]: 7-gram
        /// dictionary[6]: 8-gram
        /// </remarks>
        /// <param name="category">新聞類別</param>
        /// <param name="rank">排名數量</param>
        /// <returns></returns>
        
        public static Dictionary<string, Frequency> LoadData(int category, int rank)
        {
            //Stopwatch s = new Stopwatch();
            //s.Start(); //開始計時

            Dictionary<string, Frequency>[] dictionary = new Dictionary<string, Frequency>[7];

            // Dictionary 陣列初始化
            for (int i = 0; i < dictionary.Length; i++)
                dictionary[i] = new Dictionary<string, Frequency>();

            // SQL Query
            string query = GetQueryByCategory(category);
            
            // 連接資料庫
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["connString"].ToString());
            OleDbCommand cmd = new OleDbCommand(query, conn);
            cmd.Connection.Open();
            cmd.ExecuteNonQuery();

            DocumentCount = 0; // 查詢筆數
            OleDbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                DocumentCount++; // 每讀到一筆資料加一次

                string result = reader.GetString(0); // 取得該筆資料 content 內容

                // 先把<BR>清掉，再移除含數字或特殊字元。
                string filter = Regex.Replace(Regex.Replace(result, @"<BR>", ""), @"[\W\d_]+", "");

                // Tokenize & 加入 dictionary 
                for (int gram = 2; gram <= 8; gram++)
                {
                    for (int i = 0; i < filter.Length - gram + 1; i++)
                    {
                        string term = filter.Substring(i, gram);

                        // 判斷 Dictionary 中是否已有此 Term。若有，則 TermFrequency++ 。
                        if (dictionary[gram - 2].ContainsKey(term))
                        {
                            dictionary[gram - 2][term].TermFrequency++;

                            // 若此 Term 曾在先前的文件中出現，則 DocumentFrequency++，
                            // 並將該 Term 最後出現的文件設定為當前文件。
                            if (dictionary[gram - 2][term].LastSeen < DocumentCount)
                            {
                                dictionary[gram - 2][term].DocumentFrequency++;
                                dictionary[gram - 2][term].LastSeen = DocumentCount;
                            }
                        }
                        else // Term 未曾出現過，則在 Dictionary 中加入此 Term。
                        {
                            dictionary[gram - 2].Add(term, new Frequency(1, 1, DocumentCount));
                        }
                    }
                }
            }
            reader.Close();
            cmd.Connection.Close();
            conn.Close();

            // 移除子關鍵字
            for (int gram = 3; gram <= 8; gram++)
            {
                foreach (KeyValuePair<string, Frequency> kvp in dictionary[gram - 2])
                {
                    string subString1 = kvp.Key.Substring(0, gram - 1);
                    string subString2 = kvp.Key.Substring(1, gram - 1);

                    if (dictionary[gram - 3].ContainsKey(subString1) &&
                    dictionary[gram - 3][subString1].TermFrequency == kvp.Value.TermFrequency &&
                    dictionary[gram - 3][subString1].DocumentFrequency == kvp.Value.DocumentFrequency)
                    {
                        dictionary[gram - 3].Remove(subString1);
                    }
                    if (dictionary[gram - 3].ContainsKey(subString2) &&
                    dictionary[gram - 3][subString2].TermFrequency == kvp.Value.TermFrequency &&
                    dictionary[gram - 3][subString2].DocumentFrequency == kvp.Value.DocumentFrequency)
                    {
                        dictionary[gram - 3].Remove(subString2);
                    }
                }
            }

            // 合併 Dictionary Array
            for (int gram = 3; gram <= 8; gram++)
            {
                dictionary[0] = dictionary[0].Concat(dictionary[gram - 2]).ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            // 計算 TF-IDF
            foreach (KeyValuePair<string, Frequency> kvp in dictionary[0])
            {
                kvp.Value.TFIDF = Math.Round((1 + Math.Log10(kvp.Value.TermFrequency)) * Math.Log10(DocumentCount / kvp.Value.DocumentFrequency), 6);
            }

            // 依照 TF-IDF 值由大而小排序
            var orderedDictionary = dictionary[0].OrderByDescending(node => node.Value.TFIDF).ToDictionary(pair => pair.Key, pair => pair.Value);

            // 建立 Tf-IDF Ranking Dictionary
            Dictionary<string, Frequency> rankingDictionary = new Dictionary<string, Frequency>();
            for (int i = 0; i < rank; i++)
            {
                var item = orderedDictionary.ElementAt(i);
                var itemKey = item.Key;
                rankingDictionary.Add(itemKey, orderedDictionary[itemKey]);
            }

            //s.Stop(); //停止計時
            //QueryTime = s.Elapsed.TotalSeconds.ToString();

            return rankingDictionary;
        }

        /// <summary>
        /// 根據新聞類別取得對應的 SQL 查詢。
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private static string GetQueryByCategory(int category)
        {
            string query = "SELECT [content] FROM ke2014_sample_news_201403 WHERE ";

            switch (category)
            {
                case ENTERTAINMENT:
                    query += "[section] LIKE \"%星%\" OR [section] LIKE \"%音%\" OR [section] LIKE \"%娛%\" OR [section] LIKE \"%電%\" OR [section] LIKE \"%影%\"";
                    break;
                case SPORTS:
                    query += "[section] LIKE \"%棒球%\" OR [section] LIKE \"%運動%\" OR [section] LIKE \"%體%\"";
                    break;
                case MAINLAND:
                    query += "[section] LIKE \"%台商%\" OR [section] LIKE \"%兩岸%\"";
                    break;
                case FINANCE:
                    query += "[section] LIKE \"%經貿%\" OR [section] LIKE \"%房市%\" OR [section] LIKE \"%股市%\" OR [section] LIKE \"%財經%\"";
                    break;
                case HEALTH:
                    query += "[section] LIKE \"%健康%\" OR [section] LIKE \"%醫藥%\"";
                    break;
                case POLITICS:
                    query += "[section] LIKE \"%政治%\"";
                    break;
                case SOCIETY:
                    query += "[section] LIKE \"生活新聞\" OR [section] LIKE \"%地方%\" OR [section] LIKE \"%社會%\"";
                    break;
            }

            return query;
        }

    }

}
