using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Hex文件合并
{
    public partial class Form1 : Form
    {
        bool istiaozhengshunxu = false;
        //hex格式解析：<0x3a>[数据长度1Byte][数据地址2Byte][数据类型1Byte][数据nByte][校验1Byte]<0x0d><0x0a>
        /*
        '00' Data Record 数据
        '01' End of File Record 文件结束标志
        '02' Extended Segment Address Record 延伸段地址
        '03' Start Segment Address Record   起始延伸地址
        '04' Extended Linear Address Record 扩展线性地址 也就是基地址
        '05' Start Linear Address Record       程序起始地址也就是程序入口地址(main)
        0800 这个就是基地址(0x0800<<16)
         */
        struct DataLineMessage
        {
            public byte length;
            public UInt32 addr;
            public byte type;
            public UInt32 ExtAddr;//数据域
            public byte checksum;
        };
        struct DatalistMessage
        {
            public string type;
            public string path;
            public UInt32 staraddr;
            public UInt32 endaddr;//数据域

        };
        private void InitGridView()
        {
            // 设置列
            dataGridView1.ColumnCount = 5;
            dataGridView1.Columns[0].Name = "起始地址";
            dataGridView1.Columns[1].Name = "类型";
            dataGridView1.Columns[2].Name = "数据";
            dataGridView1.Columns[3].Name = "长度";
            dataGridView1.Columns[4].Name = "路径";
        }

        public Form1()
        {
            InitializeComponent();
            InitGridView();
            //允许拖拽
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);

            btn_help.Visible = true;
            textOutPath.Text = Application.StartupPath + @"/merge.hex";
        }
        bool ParaseFileLine(string inoutstr, out DataLineMessage formatnew)
        {
            formatnew.length = 0;
            formatnew.type = 0;
            formatnew.addr = 0;
            formatnew.ExtAddr = 0;
            formatnew.checksum = 0;
            try
            {
                //DataLineMessage line=new DataLineMessage();
                byte[] data = HexStringToByteArray(inoutstr.Substring(1));
                if ((inoutstr.Substring(0, 1) != ":"))
                {
                    return false;
                }
                if (data.Length != 1 + 2 + 1 + 1 + data[0])
                {
                    return false;
                }
                //长度
                formatnew.length = data[0];
                //数据地址
                formatnew.addr = (UInt32)((data[1] << 8) | (data[2] << 0));
                //数据类型
                formatnew.type = data[3];

                if ((formatnew.type <= 0x05) && (formatnew.type >= 0x02))
                {
                    //扩展地址
                    if (formatnew.length >= 2)
                    {
                        formatnew.ExtAddr = (UInt32)((data[4] << 8) | (data[5] << 0));
                        formatnew.ExtAddr <<= 16;
                        if (formatnew.length == 4)
                        {
                            formatnew.ExtAddr |= (UInt32)((data[6] << 8) | (data[7] << 0));
                        }
                    }
                }
                formatnew.checksum = data[data.Length - 1];
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {

            }
        }
        private string makenewline (string data, string offset,string type = "00")
        {

            string line = "";
            string cksum = "";
            data=data.Replace("0x", "").Replace("0x", "");
            offset = offset.Replace("0x", "").Replace("0x", "");
            if (data.Length % 2 == 0)
            {
                line += (data.Length/2).ToString("x2");
                if (offset!=null)
                {
                    line += offset;
                    line += type;
                    line += data;
                    CalCheckSum(line, out cksum);
                    line += cksum;
                    line = ":" + line;
                    //MessageBox.Show(line, "merge faild");
                    return line;
                    
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }


        }
        private bool CalCheckSum(string hexString, out string ckSum)

        {
            UInt32 sum1 = 0;
            if (hexString.Length > 0)
            {
                string[] Array = new string[hexString.Length ];
                for (int i = 0; i < (hexString.Length); i = i + 2)
                {
                    Array[i] = hexString.Substring(i, 2);
                    sum1 += Convert.ToUInt32(hexString.Substring(i, 2),16);

                }
                sum1 = (sum1 % 256);
                sum1 = 256 - sum1;
                ckSum = sum1.ToString("X2");
                if (ckSum.Length > 2)
                {
                    ckSum=ckSum.Substring(ckSum.Length - 2, 2);
                }
                return true;
            }
            else
            {
                MessageBox.Show("", "merge faild");
                ckSum = "";
                return false;
            }
                

        }
        private bool Checkaddr(int row ,out string addrandoffset)
        {
             addrandoffset = dataGridView1.Rows[row].Cells[2].Value.ToString().Replace("0x", "").Replace("0x", "");
            if (addrandoffset.Length % 2 == 0&& addrandoffset != "")
            {
                if(addrandoffset.Length == 8)
                {
                    return true;
                }
                
                else
                {
                    return false;
                }

            }
            else
            {
                return false;
            }
        }
        private void GetDataLenth(string hexpath, DataGridViewCell offsetcell, DataGridViewCell lenthcell)
        {
            DataLineMessage line = new DataLineMessage();
            FileStream filefd = new FileStream(hexpath, FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(filefd, Encoding.Default);
            filefd.Seek(0, SeekOrigin.Begin);
            string content = streamReader.ReadLine();
            UInt32 ExtAddr = 0;
            UInt32 lastaddr=0;
            UInt32 BaseAddr=0;
            while (null != content)
            {
                if (ParaseFileLine(content, out line))
                {
                    if (line.type == 0x04)
                    {

                        BaseAddr = line.ExtAddr;
                        ExtAddr = line.ExtAddr;
                        content = streamReader.ReadLine();
                        if (ParaseFileLine(content, out line))
                        {
                            BaseAddr |= line.addr;//加上偏移地址
                                                  //MessageBox.Show(BaseAddr.ToString("x4"), "error");
                            offsetcell.Value = "0x" + BaseAddr.ToString("X8");

                        }
                    }
                    if (line.type == 0x00)
                    {
                        lastaddr = line.addr;
                    }
                }
                content = streamReader.ReadLine();
            }
            if (filefd != null)
            {
                filefd.Close();
            }
            if (streamReader != null)
            {
                streamReader.Close();
            }
            if (lastaddr !=0)
            {
                lenthcell.Value = "0x"+(ExtAddr + lastaddr).ToString("X8");
            }
            

        }
        private void btn_merge_Click(object sender, EventArgs e)
        {
            paixuhang();
            
            StreamWriter Newfile = null;
            string savepath = GetNewPathForDupes(textOutPath.Text);
            Newfile = new StreamWriter(savepath);
        
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (dataGridView1.Rows[i].Cells[0].Value.ToString() == "文件")
                    {
                        if (System.IO.Path.GetExtension(dataGridView1.Rows[i].Cells[1].Value.ToString()) == ".hex")
                        {
                            savehex(Newfile, dataGridView1.Rows[i].Cells[4].Value.ToString());
                        }
                        else if (System.IO.Path.GetExtension(dataGridView1.Rows[i].Cells[1].Value.ToString()) == ".bin")
                        {
                            string ao = "";
                            if (Checkaddr(i, out ao))
                            {
                                savebin(Newfile, dataGridView1.Rows[i].Cells[4].Value.ToString(), ao);
                            }
                            else
                            {
                                MessageBox.Show("bin地址格式有误，请重新输入，正确格式为 0x08000000");
                            }

                    }
                    

                    }else if (dataGridView1.Rows[i].Cells[0].Value.ToString() == "数据")
                    {
                        string ao = "";
                        if (Checkaddr(i, out ao))
                        {
                            savedataline(Newfile, dataGridView1.Rows[i].Cells[1].Value.ToString(), ao);
                        }
                        else
                        {
                            MessageBox.Show("地址格式有误，正确格式为 0x08000000");
                        }

                }
                }
            Newfile.WriteLine(":00000001FF");//继续保存该行数据
            Newfile.Close();
        }
        public void savedataline(StreamWriter Newfile, string data, string offset)
        {
            Newfile.WriteLine(makenewline(offset.Substring(0, 4), "0000", "04"));
            Newfile.WriteLine(makenewline(data, offset.Substring(4)));
        }
        private void savehex(StreamWriter Newfile,string path)
        {
            StreamReader fileReader = null;
            fileReader = new StreamReader(path);
            DataLineMessage line = new DataLineMessage();
            string strline = null;
            do
            {
                strline = fileReader.ReadLine();
                if (ParaseFileLine(strline, out line))
                {
                    if (line.type == 0x01)//结束标志
                    {
                        break;
                    }
                    else if (line.type == 0x05)//入口地址
                    {
                        //Newfile.WriteLine(strline);//继续保存该行数据
                    }
                    else//数据等
                    {

                        Newfile.WriteLine(strline);//保存数据
                    }
                }
                else
                {
                    fileReader.Close();

                    MessageBox.Show("There have some error in file", "merge faild");
                    return;
                }

            } while (strline != null);

        }
        private void savebin(StreamWriter Newfile, string path, string offset)
        {
            Newfile.WriteLine(makenewline(offset.Substring(0, 4), "0000","04"));


            int file_len;//bin文件长度
            UInt32 addr = Convert.ToUInt32(offset.Substring(4));
            int count = 0;//换行显示计数
            byte[] binchar = new byte[] { };
            string data = "";




            FileStream Myfile = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader binreader = new BinaryReader(Myfile);

            file_len = (int)Myfile.Length;//获取bin文件长度

            StringBuilder str = new StringBuilder();

            binchar = binreader.ReadBytes(file_len);
            for (int j = 0;j<binchar.Length;j++)
            {
                if (count % 32 == 0&& count  != 0)
                {
                    if (data == "00") continue;
                    Newfile.WriteLine(makenewline(data, addr.ToString("x4")));
                    count = 0;
                    data = "";

                    addr+=32;
                }

                
                data += binchar[j].ToString("X2");

                if(j == binchar.Length - 1)
                {
                    Newfile.WriteLine(makenewline(data, addr.ToString("x4")));
                }
                count++;
            }
           
            binreader.Close();

        }

        #region HEX2BIN	
        public bool ReadHexFile(string fileName)
        {
            if (fileName == null || fileName.Trim() == "")  //文件存在
            {
                return false;
            }
            using (FileStream fs = new FileStream(fileName, FileMode.Open))  //open file
            {
                StreamReader HexReader = new StreamReader(fs);    //读取数据流
                string szLine = "";
                string szHex = "";
                string szAddress = "";
                string szLength = "";

                while (true)
                {
                    szLine = HexReader.ReadLine();      //读取Hex中一行
                    if (szLine == null)
                    {
                        break;//读取完毕，退出
                    }
                    int datatype = Parase_HexLineData(szLine);
                    if (datatype < 0)
                    {
                        MessageBox.Show("文件格式有误");
                        break;//解析失败
                    }
                    // if (szLine.Substring(0, 1) == ":")    //判断首字符是":"
                    {
                        if (datatype == 1) { break; }  //文件结束标识
                        //直接解析数据类型标识为 : 00 和 01 的格式
                        if (datatype == 0)
                        {
                            szHex += szLine.Substring(9, szLine.Length - 11);  //所有数据分一组 
                            szAddress += szLine.Substring(3, 4); //所有起始地址分一组
                            szLength += szLine.Substring(1, 2); //所有数据长度归一组
                        }
                    }
                }
                //将数据字符转换为Hex，并保存在数组 szBin[]
                Int32 j = 0;
                Int32 Length = szHex.Length;      //获取长度
                byte[] szBin = new byte[Length / 2];
                for (Int32 i = 0; i < Length; i += 2)
                {
                    szBin[j] = (byte)Int16.Parse(szHex.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);   //两个字符合并一个Hex 
                    j++;
                }
                //将起始地址的字符转换为Hex，并保存在数组 szAdd []
                j = 0;
                Length = szAddress.Length;      //get bytes number of szAddress
                Int32[] szAdd = new Int32[Length / 4];
                for (Int32 i = 0; i < Length; i += 4)
                {
                    szAdd[j] = Int32.Parse(szAddress.Substring(i, 4), System.Globalization.NumberStyles.HexNumber);  //两个字符合并一个Hex
                    j++;
                }
                //将长度字符转换为Hex，并保存在数组 szLen []
                j = 0;
                Length = szLength.Length;      //get bytes number of szAddress
                byte[] szLen = new byte[Length / 2];
                for (Int32 i = 0; i < Length; i += 2)
                {
                    szLen[j] = (byte)Int16.Parse(szLength.Substring(i, 2), System.Globalization.NumberStyles.HexNumber); // merge two bytes to a hex number
                    j++;
                }
                array_save_data(szAdd, szBin, szLen, Path.ChangeExtension(fileName, "bin"));   //保存为bin
            }
            return true;
        }
        private void array_save_data(Int32[] address, byte[] data, byte[] length, string Path)
        {
            //1.整理数据所需参数
            int jcount = 0;
            int max_address = (address[(address.Length) - 1]) + 16;
            byte[] all_show_data = new byte[max_address];  //存储解析完成的数据数组
            for (Int32 i = 0; i < all_show_data.Length; i++) { all_show_data[i] = 255; }  //默认全为0

            //2.从address[]数组中获取HEX对应地址
            for (Int32 i = 0; i < address.Length; i++)
            {
                if (i >= 1) { jcount += length[i - 1]; }  //从length[]数组中获取数据对应的长度大小
                for (int j = 0; j < length[i]; j++)
                {
                    all_show_data[address[i] + j] = data[jcount + j]; //all_show_data[]数组中添加数据
                }
            }
            FileStream fs = new FileStream(Path, FileMode.Create);
            fs.Write(all_show_data, 0, all_show_data.Length);
            fs.Close();
        }
        #endregion
        int Parase_HexLineData(string szLine)//返回数据类型01-05
        {
            //冒号 本行数据长度(1byte) 本行数据的起始地址(2byte) 数据类型(1byte) 数据(N byte) 校验码(1byte)
            int Len = szLine.Length - 1;

            if (Len < 10) { return -1; }

            try
            {
                if (Len % 2 == 0)
                {
                    if (szLine.Substring(0, 1) == ":")    //判断首字符是":"
                    {
                        byte[] BytesData = new byte[Len / 2];  //声明一个长度为hexstring长度一半的字节组
                        byte checksum = 0;

                        for (int i = 0; i < BytesData.Length; i++)
                        {
                            BytesData[i] = Convert.ToByte(szLine.Substring(i * 2 + 1, 2), 16);  //将hexstring的两个字符转换成16进制的字节组            
                        }
                        if (BytesData[0] != (BytesData.Length - 5))//长度域与实际长度不符
                        {
                            return -1;
                        }

                        for (int i = 0; i < BytesData.Length - 1; i++)
                        {
                            checksum += BytesData[i];
                        }
                        if (checksum == (byte)(0 - BytesData[BytesData.Length - 1]))
                        {
                            return BytesData[3];
                        }
                    }
                }
            }
            catch
            {

            }

            return -1;
        }

        #region 字符串转换函数
        //翻转byte数组
        public static void ReverseBytes(byte[] bytes)
        {
            byte tmp;
            int len = bytes.Length;

            for (int i = 0; i < len / 2; i++)
            {
                tmp = bytes[len - 1 - i];
                bytes[len - 1 - i] = bytes[i];
                bytes[i] = tmp;
            }
        }
        //规定转换起始位置和长度
        public static void ReverseBytes(byte[] bytes, int start, int len)
        {
            int end = start + len - 1;
            byte tmp;
            int i = 0;
            for (int index = start; index < start + len / 2; index++, i++)
            {
                tmp = bytes[end - i];
                bytes[end - i] = bytes[index];
                bytes[index] = tmp;
            }
        }

        // 翻转字节顺序 (16-bit)
        public static UInt16 ReverseBytes(UInt16 value)
        {
            return (UInt16)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }


        // 翻转字节顺序 (32-bit)
        public static UInt32 ReverseBytes(UInt32 value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }
        // 翻转字节顺序 (64-bit)
        public static UInt64 ReverseBytes(UInt64 value)
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                   (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                   (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                   (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }
        public string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            return sb.ToString().ToUpper();
        }

        public byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            if (s.Length % 2 != 0)
            {
                s = s.Substring(0, s.Length - 1) + "0" + s.Substring(s.Length - 1);
            }
            byte[] buffer = new byte[s.Length / 2];

            try
            {
                for (int i = 0; i < s.Length; i += 2)
                    buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
                return buffer;
            }
            catch
            {
                string errorString = "E4";
                byte[] errorData = new byte[errorString.Length / 2];
                errorData[0] = (byte)Convert.ToByte(errorString, 16);
                return errorData;
            }
        }

        public string StringToHexString(string s)
        {
            s = s.Replace(" ", "");
            string buffer = "";
            char[] myChar;
            myChar = s.ToCharArray();
            for (int i = 0; i < s.Length; i++)
            {
                buffer = buffer + Convert.ToString(myChar[i], 16);
                buffer = buffer.ToUpper();
            }
            return buffer;
        }
        #endregion
        /// <summary>
        /// Generates a new path for duplicate filenames.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private string GetNewPathForDupes(string path)
        {
            string newFullPath = path.Trim();
            //if (System.IO.File.Exists(path))
            //    MessageBox.Show("存在");
            //else
            //    MessageBox.Show("不存在");
            if (System.IO.File.Exists(path))
            {
                string directory = Path.GetDirectoryName(path);
                string filename = Path.GetFileNameWithoutExtension(path);
                string extension = Path.GetExtension(path);
                int counter = 1;
                do
                {
                    string newFilename = string.Format("{0}({1}){2}", filename, counter, extension);
                    newFullPath = Path.Combine(directory, newFilename);
                    counter++;
                } while (System.IO.File.Exists(newFullPath));
                return newFullPath;
            }
            return path;
        }
        private void btn_outpath_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveImageDialog = new SaveFileDialog();
            saveImageDialog.FileName = "merge";
            saveImageDialog.Title = "保存";
            saveImageDialog.Filter = @"HEX文件|*.hex";
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveImageDialog.FileName.ToString();
                textOutPath.Text = fileName;
            }
        }
        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.All;
            else e.Effect = DragDropEffects.None;
        }
        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            //获取第一个文件名
            string fileName = (e.Data.GetData(DataFormats.FileDrop, false) as String[])[0];
            int index = this.dataGridView1.Rows.Add();
            int row = dataGridView1.Rows.Count-1;

            dataGridView1.Rows[row].Cells[0].Value = "文件";
            dataGridView1.Rows[row].Cells[4].Value = fileName;
            dataGridView1.Rows[row].Cells[1].Value = System.IO.Path.GetFileName(fileName);
            if (System.IO.Path.GetExtension(fileName) == ".hex")
            {
                GetDataLenth(fileName, dataGridView1.Rows[row].Cells[2], dataGridView1.Rows[row].Cells[3]);
            }
            else if (System.IO.Path.GetExtension(fileName) == ".bin")
            {
                dataGridView1.Rows[row].Cells[2].ReadOnly = false;
                MessageBox.Show("请手动输入bin地址起始值", "error");
            }
            


        }

        private void btn_help_Click(object sender, EventArgs e)
        {
            MessageBox.Show("先在第一个格子选择输入类型，文件支持输入地址格式0x08000000\r\n可拖拽hex、bin文件到对话框\r\n可以点击排序按钮根据文件起始地址进行排序\r\n" +
                "没有文件长度检查功能，请自行确定地址会不会互相覆盖\r\n", "说明");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int index = this.dataGridView1.Rows.Add();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count>0)  //是否空表，不加判断遇到空表将出错
            {
                dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);  //删除一行
                dataGridView1.Refresh();  //刷新显示
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            /*
            *这里可以加入判断是不是需要立刻提交，如果不用就返回
            */
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            
            if (istiaozhengshunxu) return;
            if (e.RowIndex == -1) return;
            DataGridViewCell cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            try
            {
                if(cell.Value.ToString()=="")
                { return; }
            }
            catch { return; }
            if (cell.Value.ToString() == "文件")
            {
                DataGridViewButtonCell txtCell = new DataGridViewButtonCell();
                dataGridView1.Rows[e.RowIndex].Cells[1] = txtCell;
                dataGridView1.Rows[e.RowIndex].Cells[1].Value = "载入文件";
                dataGridView1.Rows[e.RowIndex].Cells[1].ReadOnly =false;
            }
            if (cell.Value.ToString() == "数据")
            {
                DataGridViewTextBoxCell txtCell1 = new DataGridViewTextBoxCell();
                dataGridView1.Rows[e.RowIndex].Cells[1] = txtCell1 ;
                dataGridView1.Rows[e.RowIndex].Cells[1].ReadOnly = false;
            }
            


        }
       

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (this.dataGridView1.CurrentCell.ColumnIndex == 2)
            {
                e.Control.KeyPress += new KeyPressEventHandler(DataOnlyHex_KeyPress);
            }
            if (this.dataGridView1.CurrentCell.ColumnIndex == 1)
            {
                e.Control.KeyPress += new KeyPressEventHandler(DataOnlyHex_KeyPress);
            }

        }
      
        private void DataOnlyHex_KeyPress(object sender, KeyPressEventArgs e)
        {
            string s = "0123456789ABCDEFXabcdefx0";
            string[] sArray = s.Split();
            if ((e.KeyChar >= 48 && e.KeyChar <= 57) || e.KeyChar == 8)
            {
                e.Handled = false;


            }
            else
            {
                if ((e.KeyChar >= 'A' && e.KeyChar <= 'F') || e.KeyChar == 8 )
                {
                    e.Handled = false;
                }
                else
                {
                    if ((e.KeyChar >= 'a' && e.KeyChar <= 'f') || e.KeyChar == 8)
                    {
                        e.Handled = false;
                    }
                    else if ((e.KeyChar == 'x') || e.KeyChar == 8)
                    {
                        e.Handled = false;
                    }else if ((e.KeyChar == 'X') || e.KeyChar == 8)
                    {
                        e.Handled = false;
                    }
                    else e.Handled = true;
                }
            }


        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewCell cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                DataGridViewColumn column = dataGridView1.Columns[e.ColumnIndex];
                if (column is DataGridViewButtonColumn)
                {
                    if (cell.Value.ToString() == "载入文件")
                    {
                        OpenFileDialog open_fd = new OpenFileDialog();
                        open_fd.Multiselect = true;
                        open_fd.Title = @"请选择文件";
                        open_fd.Filter = @"所有文件(*.*)|*.*|HEX文件(*.hex)|*.hex|BIN文件(*.bin)|*.bin";
                        open_fd.FilterIndex = 1;
                        if (open_fd.ShowDialog() == DialogResult.OK)
                        {
                            dataGridView1.Rows[e.RowIndex].Cells[4].Value = open_fd.FileNames[0];
                            dataGridView1.Rows[e.RowIndex].Cells[1].Value = System.IO.Path.GetFileName(open_fd.FileNames[0]);
                            if (System.IO.Path.GetExtension(open_fd.FileNames[0]) == ".hex")
                            {
                                GetDataLenth(open_fd.FileNames[0], dataGridView1.Rows[e.RowIndex].Cells[2], dataGridView1.Rows[e.RowIndex].Cells[3]);
                            }
                            else if (System.IO.Path.GetExtension(open_fd.FileNames[0]) == ".bin")
                            {
                                dataGridView1.Rows[e.RowIndex].Cells[2].ReadOnly = false;
                                MessageBox.Show("请手动输入bin地址起始值", "error");
                            }
                        }
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            paixuhang();
        }
        private void paixuhang()
        {
            UInt32[] list = new UInt32[dataGridView1.Rows.Count];
            int[] list1 = new int[dataGridView1.Rows.Count];
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                string ao = "";
                if (Checkaddr(i, out ao))
                {
                    list[i] = (Convert.ToUInt32(ao, 16));
                    list1[i] = i;
                }
                else
                {
                    MessageBox.Show("地址格式有误，正确格式为 0x08000000");
                }
            }
            Array.Sort(list, list1);
            int nCount = dataGridView1.Rows.Count;
            for (int c = 0; c < list1.Length; c++)
            {
                DataGridViewRow dgvr = dataGridView1.Rows[list1[c]];
                istiaozhengshunxu = true;

                dataGridView1.Rows.Add();
                for (int p = 0; p < dataGridView1.Columns.Count; p++)
                {
                    if (dataGridView1.Rows[list1[c]].Cells[p].Value != null)
                    {
                        dataGridView1.Rows[nCount + c].Cells[p].Value = dataGridView1.Rows[list1[c]].Cells[p].Value.ToString();
                    }

                }

            }
            for (int u = 0; u < nCount; u++)
            {
                dataGridView1.Rows.RemoveAt(0);
            }

            dataGridView1.Refresh();
            istiaozhengshunxu = false;

        }
    }
}