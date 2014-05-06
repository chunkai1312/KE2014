using KE2014.Models;
using KE2014.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KE2014.Controllers
{
    public class VSMController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controller = filterContext.Controller;
            if (controller != null)
            {
                var timer = new Stopwatch();
                controller.ViewData["_ActionTimer"] = timer;
                timer.Start();
            }
            base.OnActionExecuting(filterContext);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var controller = filterContext.Controller;
            if (controller != null)
            {
                var timer = (Stopwatch)controller.ViewData["_ActionTimer"];
                if (timer != null)
                {
                    timer.Stop();
                    controller.ViewData["_ElapsedTime"] = timer.Elapsed.TotalSeconds;
                }
            }
        }

        public ActionResult Index(int id)
        {
            // 查詢文件ID
            string[] QUERY_DOCS = { "1393924219057_1_N01", "1393793583281_N01", "1393792680648_N01", "1393903770008_N01", "1394180338658_N01", 
                                    "1394105127891_N01", "1393816909339_N01", "1393619466219_N01", "1393619409139_N01", "1394051127145_N01" };

            List<string> keywordList = Models.VSMContext.GetKeywords(); // 取得關鍵字
            Dictionary<string, Document> documents = Models.VSMContext.GetDocuments(); // 取得文件內容 (DocID => Document)
            Dictionary<string, List<string>> keywordDocs; // 關鍵字所在文件 (Keyword => DocList)
            Dictionary<string, List<Axis>> docVectors; // 文件向量 (DocID => Vector)


            if (Models.VSMContext.IsExistKeywordDocsFile()) // 檢查是否存在 keyword_docs.txt 
            {
                string json = Models.VSMContext.ReadKeywordDocsFile(); // 讀取 keyword_docs.txt (JSON format)
                keywordDocs = (Dictionary<string, List<string>>)JsonConvert.DeserializeObject(json, typeof(Dictionary<string, List<string>>));
            }
            else // 若無, 則建立關鍵字所在文件之 Dictionary 並寫入 keyword_docs.txt
            {
                keywordDocs = Models.VSMContext.GetKeywordDocs(documents, keywordList); // 取得關鍵字
                string json = JsonConvert.SerializeObject(keywordDocs); // 將 Dictionary 序列化為 JSON 格式
                Models.VSMContext.WriteKeywordDocsFile(json); // 寫入檔案
            }


            if (Models.VSMContext.IsExistDocVectorsFile()) // 檢查是否存在 doc_vertors.txt
            {
                string json = Models.VSMContext.ReadDocVectorsFile(); // 讀取 doc_vertors.txt (JSON format)
                docVectors = (Dictionary<string, List<Axis>>)JsonConvert.DeserializeObject(json, typeof(Dictionary<string, List<Axis>>));          
            }
            else // 若無, 則建立文件向量之 Dictionary 並寫入 doc_vertors.txt
            {
                docVectors = Models.VSMContext.GetDocVectors(documents, keywordList); // 取得文件向量
                docVectors = Models.VSMContext.CalcTFIDF(docVectors, documents, keywordDocs); // 計算 TF-IDF
                string json = JsonConvert.SerializeObject(docVectors); // 將 Dictionary 序列化為 JSON 格式
                Models.VSMContext.WriteDocVectorsFile(json); // 寫入檔案
            }

            // 設置查詢文件
            string queryID = QUERY_DOCS[id - 1];
            Document queryDoc = Models.VSMContext.GetDocuments()[queryID];
            ViewBag.QueryID = queryID;
            ViewBag.QuerySource = queryDoc.Source;
            ViewBag.QuerySection = queryDoc.Section;
            ViewBag.QueryTitle = queryDoc.Title;

            // 取出查詢文件關鍵字
            string keywords = null;
            foreach (KE2014.Models.Axis axis in docVectors[queryID])
            {
                keywords += axis.Term + " ";
            }
            ViewBag.QueryKeywords = keywords;

            List<SimilarDoc> similarDocs = Models.VSMContext.GetSimilarDocs(queryID, 6, docVectors, documents); // 取得相似文件之 List
            string firstSimilarDocID = similarDocs[0].ID; // 取得第一筆相似文件ID
            List<Sentence> summary = Models.VSMContext.GetDocSummary(documents[firstSimilarDocID].Content, 3, keywordList); // 取得第一筆相似文件摘要(關鍵句)           

            VSMIndexViewModel viewModel = new VSMIndexViewModel(); // 實體化 viewModel, 作為傳入 view 之用
            viewModel.SimilarDocs = similarDocs;
            viewModel.Summary = summary;

            return View(viewModel);
        }
    }
}
