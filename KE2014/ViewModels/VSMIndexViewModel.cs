using KE2014.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KE2014.ViewModels
{
    public class VSMIndexViewModel
    {
        public List<SimilarDoc> SimilarDocs { get; set; }
        public List<Sentence> Summary { get; set; }
    }

}