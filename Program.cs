﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MergeExcel {
    static class Program {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            // Application.Run( new Form_MergeExcel() );
            // Application.Run( new Form_Merge_FinaAnalyPlus() );
            Application.Run( new Form_TeenScore() );
            //Application.Run( new Form_RegexExam() );
            //Application.Run( new Form_RegexExam() );
        }
    }
}
