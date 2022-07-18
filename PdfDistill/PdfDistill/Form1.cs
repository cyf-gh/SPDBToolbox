﻿using iTextSharp.text.pdf.parser;

using Microsoft.VisualBasic;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PdfDistill {
    public partial class Form1 : Form {
        List<Dictionary<string, string>> pages = new List<Dictionary<string, string>>();
        string Log = string.Empty;
        List<Dictionary<string, string>> Res = new List<Dictionary<string, string>>();
        List<string> UnsolvedPdfs = new List<string>();
        List<int> UnsolvedNumber = new List<int>();
        public void RefreshStatus() {
            lb_Status.Text = $"已汇总数据笔数：{Res.Count}      未处理文件个数：{UnsolvedPdfs.Count}";
        }
        public void GenerateExcel() {
            var xlApp = new Microsoft.Office.Interop.Excel.Application();
            var xlwb = xlApp.Workbooks.Add();
            var xlws = xlwb.Worksheets.get_Item( 1 ) as Microsoft.Office.Interop.Excel.Worksheet;

            //var hs = File.ReadAllText( "./tableHeads.txt" );
            int j = 1;
            //var hss = hs.Split( ',' );
            // 创建表头
            var hs = new List<string>();
            foreach ( var h in pages[0] ) {
                hs.Add(h.Key);
                xlws.Cells[1, j] = h.Key;
                ++j;
            }

            for ( int i = 0; i < pages.Count; i++ ) {
                for ( int ii = 0; ii < pages[i].Count; ii++ ) {
                    xlws.Cells[ i+2, ii +1 ] = '\t'+pages[i][hs[ii]];
                }
                //xlws.Cells[i + 2, ii + 1] = Res[i][hss[ii]];
            }
            var fileFullPath = System.IO.Path.Combine( Environment.CurrentDirectory, $"结果-{DateTime.Now.ToString( "MM-dd-yyyy-HH-mm-ss" )}.xlsx" );
        SAVE:
            try {
                xlwb.SaveCopyAs( fileFullPath );
            } catch ( Exception ex ) {
                if ( DialogResult.OK != MessageBox.Show( ex.Message + "\n请重试\n按取消中止", Text, MessageBoxButtons.OKCancel ) ) {
                    return;
                }
                goto SAVE;
            }
            Process.Start( fileFullPath );
        }
        public Form1() {
            InitializeComponent();
        }
        public void WriteLog( string log ) {
            tb_log.Text += $"{log}{Environment.NewLine}";
        }
        public static string ReadFile( string file, string pswd = "" ) {
            if ( string.IsNullOrEmpty( file ) )
                throw new ArgumentNullException( "file", "file cannot be null or empty" );
            FileInfo info = new FileInfo( file );
            if ( !info.Exists )
                throw new FileNotFoundException( "file must exist" );
            if ( !info.Extension.Equals( ".pdf", StringComparison.OrdinalIgnoreCase ) )
                throw new ArgumentException( "File must have a .pdf extension" );

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            iTextSharp.text.pdf.PdfReader reader = null;
            try {
                reader = new iTextSharp.text.pdf.PdfReader( info.FullName, Encoding.ASCII.GetBytes( pswd ) );
                for ( int i = 1; i <= reader.NumberOfPages; i++ ) {
                    ITextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                    string currentText = PdfTextExtractor.GetTextFromPage( reader, i, strategy );
                    builder.Append( currentText );
                }
            } catch ( Exception ex ) {
                throw ex;
            } finally {
                if ( reader != null )
                    reader.Close();
            }

            return builder.ToString();
        }
        private void Form1_Load( object sender, EventArgs e ) {
            RefreshPswd();
            RefreshTicktsFolderPath();
            RefreshStatus();
        }
        #region 初期设置
        void RefreshPswd() {
            lb_pswd.Text = File.ReadAllText( "./pswd.txt" );
        }
        void RefreshTicktsFolderPath() {
            lb_ticketsPath.Text = File.ReadAllText( "./ticketsFolderPath.txt" );
        }
        private void btn_newPswd_Click( object sender, EventArgs e ) {
            var newPswd = Interaction.InputBox( "输入密码", $"请输入新的pdf密码" );
            if ( !string.IsNullOrWhiteSpace( newPswd ) ) {
                File.WriteAllText( "./pswd.txt", newPswd );
                MessageBox.Show( "密码修改成功", "提示" );
                RefreshPswd();
            }
        }
        private void btn_selectDir_Click( object sender, EventArgs e ) {
            var fbd = new FolderBrowserDialog();
            fbd.Description = "选取票据所在的文件夹";
            if ( fbd.ShowDialog() == DialogResult.OK ) {
                lb_ticketsPath.Text = fbd.SelectedPath;
                File.WriteAllText( "./ticketsFolderPath.txt", lb_ticketsPath.Text );
                MessageBox.Show( $"已更新票据文件夹路径为\n{fbd.SelectedPath}" );
            }
        }
        #endregion
        private void btn_Start_Click( object sender, EventArgs e ) {
            UnsolvedPdfs.Clear();
            var ticketsDI = new DirectoryInfo( lb_ticketsPath.Text );
            if ( !ticketsDI.Exists ) {
                MessageBox.Show( "设置的票据文件夹不存在，请重新设置" );
                return;
            }
            var ts = ticketsDI.GetFiles( "*.pdf" );
            if ( MessageBox.Show( $"目录下有 {ts.Length} 个PDF文件，是否继续？" ) != DialogResult.OK ) {
                MessageBox.Show( "操作终止" );
                return;
            }
            foreach ( var f in ts ) {
                string filePath = f.FullName;
                string a = string.Empty;
                int tryCount = 0;
                const int PswdCorrect = -1;
                WriteLog( "开始转化！" );
                while ( tryCount != PswdCorrect ) {
                    try {
                        WriteLog( $"正在处理文件：{f.FullName}" );
                        a = ReadFile( filePath, tryCount > 0 ?
                            Interaction.InputBox( "请输入pdf密码", $"输入\"{filePath}\"密码" )
                            : File.ReadAllText( "./pswd.txt" ) );
                        WriteLog( $"数据处理成功" );
                        tryCount = PswdCorrect;
                    } catch ( Exception ex ) {
                        if ( DialogResult.OK !=
                            MessageBox.Show(
                            $"出现了一些问题{Environment.NewLine}详情：{ex.Message}{Environment.NewLine}按“取消”跳过该文件处理下一个文件，按“确定”重试PDF密码",
                            "", MessageBoxButtons.OKCancel ) ) {
                            break;
                        }
                        WriteLog( $"密码不正确，请重新输入密码；重试次数：{tryCount}" );
                        if ( ex.Message == "Bad user password" ) {
                            MessageBox.Show( $"文档：\"{filePath}\"的密码不正确，请重新输入密码并重试。", "密码错误" );
                            tryCount++;
                        }
                    }
                }
                if ( a == String.Empty ) {
                    UnsolvedPdfs.Add( f.FullName );
                    continue;
                }
                char[] splits = { '\n', ' ' };
                var singles = Regex.Split( a, "可通过我行官网、VTM等渠道录入电子印章序列号验证回单信息。", RegexOptions.IgnoreCase );
                foreach ( var s in singles ) {
                    var lines = s.Split( '\n' );
                    var p = new Dictionary<string, string>();
                    foreach ( var line in lines ) {
                        if ( line.Contains( "交易日期：" ) || line.Contains( "网点编号：" ) ) {
                            var pairs = line.Split( ' ' );
                            foreach ( var pair in pairs ) {
                                var pp = pair.Split( '：' );
                                if ( pp.Length == 2 ) {
                                    p.Add( pp[0], pp[1] );
                                } else {
                                    UnsolvedNumber.Add( singles.ToList().IndexOf( s ) );
                                }
                            }
                        } else {
                            var pp = line.Split( '：' );
                            if ( pp.Length == 2 ) {
                                p.Add( pp[0], pp[1] );
                            } else {
                                UnsolvedNumber.Add( singles.ToList().IndexOf( s ) );
                            }
                        }
                    }
                    pages.Add( p );
                }

                // var pairs = a.Split(splits);
                //var r = new Dictionary<string, string>();

                //WriteLog($"开始处理数据，数据笔数：{pairs.Length}");
                //foreach (var p in pairs)
                //{
                //    var b = p.Split('：');
                //    if (b.Length == 2)
                //    {
                //        if (!string.IsNullOrWhiteSpace(b[1]))
                //        {
                //            r[b[0]] = b[1];
                //        }
                //    }
                //}
                WriteLog($"数据处理完毕，实际数据笔数：{pages.Count}");
                // Res.Add( r );
                // WriteLog( $"当前文件处理完毕，总数据笔数{Res.Count}，未处理笔数{UnsolvedPdfs.Count}，未匹配笔数：{UnsolvedNumber.Count}" );
                WriteLog( $"" );
            }
            RefreshStatus();
            if ( UnsolvedPdfs.Count != 0 ) {
                MessageBox.Show( $"有{UnsolvedPdfs.Count}个未处理的文件，未处理的文件将以文本的形式呈现" );
                string strUsp = "";
                foreach ( var usp in UnsolvedPdfs ) {
                    strUsp += $"{usp}{Environment.NewLine}";
                }
                File.WriteAllText( "./unsolvedPdfs.txt", strUsp );
                Process.Start( System.IO.Path.Combine( Environment.CurrentDirectory, "unsolvedPdfs.txt" ) );
            }
            if ( DialogResult.OK == MessageBox.Show( "数据已装载，是否立即生成Excel表格？", "提示", MessageBoxButtons.OKCancel ) ) {
                GenerateExcel();
            }
        }

        private void btn_GenExcel_Click( object sender, EventArgs e ) {
            if ( Res.Count != 0 ) {
                GenerateExcel();
            } else {
                MessageBox.Show( "数据暂时为空，请先点击开始转化生成数据", Text );
            }
        }
    }
}
