using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace KE2014.Models
{
    /// <summary>
    /// 代表文件內容的類別
    /// </summary>
    public class Document
    {
        public string Source { get; set; }
        public string Section { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string PageURL { get; set; }
        public string PostTime { get; set; }
        public string Content { get; set; }

        public Document(string source, string section, string title, string author, string pageURL, string postTime, string content)
        {
            this.Source = source;
            this.Section = section;
            this.Title = title;
            this.Author = author;
            this.PageURL = pageURL;
            this.PostTime = postTime;
            this.Content = content;
        }
    }

    /// <summary>
    /// 代表分量的類別
    /// </summary>
    public class Axis : IComparable
    {
        public string Term { get; set; }
        public double TFIDF { get; set; }

        public Axis(string term)
        {
            this.Term = term;
        }

        public int CompareTo(object obj)
        {
            Axis other = (Axis)obj;
            if (this.TFIDF > other.TFIDF) { return -1; }
            if (this.TFIDF < other.TFIDF) { return 1; }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// 代表相似文件的類別
    /// </summary>
    public class SimilarDoc : IComparable
    {
        public string ID { get; set; }
        public string Source { get; set; }
        public string Section { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public double Score { get; set; }
        public List<string> Keywords { get; set; }

        public SimilarDoc(string id, string source, string section, string title, string content, double score, List<string> keywords)
        {
            this.ID = id;
            this.Source = source;
            this.Section = section;
            this.Title = title;
            this.Content = content;
            this.Score = score;
            this.Keywords = keywords;
        }

        public int CompareTo(object obj)
        {
            SimilarDoc other = (SimilarDoc)obj;
            if (this.Score > other.Score) { return -1; }
            if (this.Score < other.Score) { return 1; }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// 代表關鍵句(摘要)的類別
    /// </summary>
    public class Sentence : IComparable
    {
        public string Text { get; set; }
        public double Score { get; set; }
        public int Order { get; set; }

        public Sentence(string text, double score, int order)
        {
            this.Text = text;
            this.Score = score;
            this.Order = order;
        }

        public int CompareTo(object obj)
        {
            Sentence other = (Sentence)obj;
            if (this.Score > other.Score) { return -1; }
            if (this.Score < other.Score) { return 1; }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// VSMModel 主要類別
    /// </summary>
    public class VSMContext
    {        
        /// <summary>
        /// 取得各文件向量
        /// </summary>
        /// <param name="doccuments"></param>
        /// <param name="keywordList"></param>
        /// <returns></returns>
        public static Dictionary<string, List<Axis>> GetDocVectors(Dictionary<string, Document> doccuments, List<string> keywordList)
        {
            Dictionary<string, List<Axis>> docVectors = new Dictionary<string, List<Axis>>();

            foreach (KeyValuePair<string, Document> kvp in doccuments)
            {
                List<Axis> vector = new List<Axis>(); 
                string id = kvp.Key;
                string content = kvp.Value.Content;

                foreach (string keyword in keywordList)
                {
                    if (content.Contains(keyword)) 
                    {
                        Axis axis = new Axis(keyword);
                        vector.Add(axis);
                    }
                }
                if (vector.Count > 0) 
                {
                    docVectors.Add(id, vector);
                }
            }

            return docVectors;
        }

        /// <summary>
        /// 取得關鍵字所在文件(某一關鍵字在哪些文件中)
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="keywordList"></param>
        /// <returns></returns>
        public static Dictionary<string, List<string>> GetKeywordDocs(Dictionary<string, Document> documents, List<string> keywordList)
        {
            Dictionary<string, List<string>> keywordDocs = new Dictionary<string, List<string>>();

            foreach (string keyword in keywordList)
            {
                List<string> docList = new List<string>();

                foreach (KeyValuePair<string, Document> kvp in documents)
                {
                    string id = kvp.Key;
                    string content = kvp.Value.Content;
                    if (content.Contains(keyword))
                    {
                        docList.Add(id);
                    }
                }
                keywordDocs.Add(keyword, docList);
            }
            return keywordDocs;
        }

        /// <summary>
        /// 計算 TF-IDF
        /// </summary>
        /// <param name="docVectors"></param>
        /// <param name="documents"></param>
        /// <param name="keywordDocs"></param>
        /// <returns></returns>
        public static Dictionary<string, List<Axis>> CalcTFIDF(Dictionary<string, List<Axis>> docVectors, 
            Dictionary<string, Document> documents,
            Dictionary<string, List<string>> keywordDocs)
        {
            Dictionary<string, List<Axis>> calcVectors = docVectors;

            foreach (KeyValuePair<string, List<Axis>> kvp in calcVectors)
            {
                string docID = kvp.Key;
                string content = documents[docID].Content;

                foreach (Axis axis in kvp.Value)
                {
                    string term = axis.Term;
                    int df = keywordDocs[term].Count; // 計算 DF
                    double idf = Math.Log(documents.Count() / df); // 計算 IDF
                    double tf = Regex.Matches(content, term).Count; // 計算原始 TF
                    tf = 1 + Math.Log(tf); // 計算修正後 TF
                    double tfidf = tf * idf; // 計算 TF-IDF
                    axis.TFIDF = tfidf;
                }
            }

            return calcVectors;
        }

        /// <summary>
        /// 計算文件相似度(Cosine)
        /// </summary>
        /// <param name="vectorA"></param>
        /// <param name="vectorB"></param>
        /// <returns></returns>
        public static double CalcSimilarity(List<Axis> vectorA, List<Axis> vectorB)
        {
            // Calculate the dot product of two vectors
            double dotProduct = 0.0;
            for (int i = 0; i < vectorA.Count; i++)
            {
                string keywordA = vectorA[i].Term;

                for (int j = 0; j < vectorB.Count(); j++)
                {
                    string keywordB = vectorB[j].Term;

                    if (keywordB.Equals(keywordA))
                    {
                        dotProduct += vectorA[i].TFIDF * vectorB[j].TFIDF;
                        break;
                    }
                }
            }

            // Normalize the first vector
            double sumVectorA = 0.0;
            foreach (Axis axis in vectorA)
            {
                sumVectorA += Math.Pow(axis.TFIDF, 2);
            }
            double normVectorA = Math.Sqrt(sumVectorA);

            // Normalize the second vector
            double sumVectorB = 0.0;
            foreach (Axis axis in vectorB)
            {
                sumVectorB += Math.Pow(axis.TFIDF, 2);
            }
            double normVectorB = Math.Sqrt(sumVectorB);

            if ((normVectorA * normVectorB) > 0)
                return dotProduct / (normVectorA * normVectorB);
            else
                return 0;
        }

        /// <summary>
        /// 取得相似文件
        /// </summary>
        /// <param name="queryID"></param>
        /// <param name="rank"></param>
        /// <param name="docVectors"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        public static List<SimilarDoc> GetSimilarDocs(string queryID, int rank, Dictionary<string, List<Axis>> docVectors, Dictionary<string, Document> documents)
        {
            // 若找不到該筆查詢文件, 則回傳 null
            if (docVectors[queryID] == null) return null;

            List<SimilarDoc> similarDocs = new List<SimilarDoc>(); // 相似文件 List
            List<Axis> queryDocVector = docVectors[queryID]; // 查詢文件向量
            
            foreach (KeyValuePair<string, List<Axis>> kvp in docVectors)
            {
                string docID = kvp.Key; // 文件ID
                List<Axis> vector = kvp.Value; // 文件向量

                string source = documents[docID].Source;
                string section = documents[docID].Section;
                string title = documents[docID].Title;
                string content = documents[docID].Content;
                double score = CalcSimilarity(queryDocVector, vector); // 計算兩文件相似度(Cosine)
                
                // 取出關鍵字
                List<string> keywords = new List<string>();
                foreach (Axis axis in kvp.Value)
                {
                    keywords.Add(axis.Term);
                }
                SimilarDoc document = new SimilarDoc(docID, source, section, title, content, score, keywords);

                if (docID != queryID) // 同篇文件不取
                {
                    similarDocs.Add(document);
                }

                similarDocs.Sort(); // 按 Score 大小排序

                if (similarDocs.Count >= rank)
                {
                    similarDocs.RemoveRange(rank, similarDocs.Count - rank); // 移除排名數量之後的文件
                }
            }

            return similarDocs;
        }

        /// <summary>   
        /// 取得文件摘要(關鍵句)
        /// </summary>
        /// <param name="content"></param>
        /// <param name="sentCount"></param>
        /// <param name="keywordList"></param>
        /// <returns></returns>
        public static List<Sentence> GetDocSummary(string content, int sentCount, List<string> keywordList)
        {
            List<Sentence> sentenceList = new List<Sentence>();

            string[] delimiter = { "。" };

            // Tokenize sentences
            string[] sentences = content.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < sentences.Count(); i++)
            {
                sentences[i] = Regex.Replace(sentences[i], @"<BR>", ""); // 句子含 <BR> 清掉
                sentences[i] = Regex.Replace(sentences[i], @"「", ""); 
                sentences[i] = Regex.Replace(sentences[i], @"」", ""); 

                int sentKeyword = 0; // 句中包含的關鍵字數量

                foreach (string keyword in keywordList)
                {
                    sentKeyword += Regex.Matches(sentences[i], keyword).Count;
                }
                double score = Math.Pow(sentKeyword, 2) / sentences[i].Length; // (句中包含的關鍵字數量)^2 / 句子長度

                if (score > 0) // 分數大於 0 才加入
                {
                    Sentence sentence = new Sentence(sentences[i], score, i);
                    sentenceList.Add(sentence);
                }
            }

            sentenceList.Sort(); // 按 Score 排序
            if (sentenceList.Count >= sentCount)
            {
                sentenceList.RemoveRange(sentCount, sentenceList.Count - sentCount); // 移除指定句數之後的關鍵句
            }
            sentenceList.Sort((x, y) => { return x.Order.CompareTo(y.Order); }); // 按句序排序

            return sentenceList;
        }


        /// <summary>
        /// 取得資料庫所有文件
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, Document> GetDocuments()
        {
            Dictionary<string, Document> documentList = new Dictionary<string, Document>();

            string query = "SELECT * FROM ke2014_sample_news_201403"; // SQL 語法

            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["connString"].ToString());
            OleDbCommand cmd = new OleDbCommand(query, conn);
            cmd.Connection.Open();
            cmd.ExecuteNonQuery();

            OleDbDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string id = reader.GetString(0);
                string source = reader.GetString(1);
                string section = reader.GetString(2);
                string title = reader.GetString(3);
                // string author = reader.GetString(4) 產生不明錯誤
                string author = null;
                string pageURL = reader.GetString(5);
                string postTime = reader.GetString(6);
                string content = reader.GetString(7);

                Document document = new Document(source, section, title, author, pageURL, postTime, content);
                documentList.Add(id, document);
            }
            reader.Close();
            cmd.Connection.Close();
            conn.Close();

            return documentList;
        }

        /// <summary>
        /// 取得關鍵字列表
        /// </summary>
        /// <returns></returns>
        public static List<string> GetKeywords()
        {
            List<string> keywords = new List<string>();

            string path = HttpContext.Current.Server.MapPath("~/Content/files/keywords.txt");

            StreamReader reader = new StreamReader(path, Encoding.UTF8);
            string line = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (!keywords.Contains(line))
                {
                    keywords.Add(line);
                }
            }
            reader.Close();

            return keywords;
        }
    }
}