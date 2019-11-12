using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Web;

namespace WindowsService
{
    public class ResloveReqs
    {
        public SqlConnection conn;
        public string userFilePath = "";
        public string goodFilePath = "";

        public ResloveReqs()
        {
            string connString = ConfigurationManager.AppSettings["connectionString"].ToString().Trim();
            conn = new SqlConnection(connString);
        }

        public void deleteOldFiles(string posid)
        {
            string strFolderPath = ConfigurationManager.AppSettings["filePath"].ToString().Trim();
            DirectoryInfo dyInfo = new DirectoryInfo(strFolderPath);
            //获取文件夹下所有的文件
            foreach (FileInfo feInfo in dyInfo.GetFiles())
            {
                if (feInfo.Name.IndexOf(posid) > -1)
                    feInfo.Delete();
            }
        }

        public int GenerateFiles(string posID)
        {
            string filePath = ConfigurationManager.AppSettings["filePath"].ToString().Trim();
            string time = DateTime.Now.Second.ToString() + posID + ".txt";

            userFilePath = filePath + "user" + time;
            goodFilePath = filePath + "data" + time;

            deleteOldFiles(posID);

            if (GetUserFile(posID, userFilePath) == 1)
            {
                Log.writelog("pos:" + posID + ",生成用户信息文件失败！\t\n");
                return 1;
            }
                

            if (GetGoodsFile(posID, goodFilePath) == 1)
            {
                Log.writelog("pos:" + posID + "，生成商品信息文件失败！\t\n");
                return 1;
            }
                

            string httpPath = ConfigurationManager.AppSettings["httpPath"].ToString().Trim();

            userFilePath = httpPath + "user" + time;
            goodFilePath = httpPath + "data" + time;

            return 0;


        }

        /// <summary>
        /// 获取犯人信息
        /// </summary>
        /// <param name="posID">pos机编号</param>
        private int GetUserFile(string posID, string filePath)
        {
            string sel_sql = " select a.prisonerno,a.prisonername,a.Abalance,a.Bbalance,a.Zbalance,a.permitinfo, "
                           + " (select count(a.prisonerno) from loadposinfo a ,prisoner c "
                           + " where a.prisonerno = c.prisonerno "
                           + "       and c.department in (select departmentid from departmentpos where posid = '" + posID + "'))  as num "
                           + " from loadposinfo a ,prisoner c "
                           + " where a.prisonerno = c.prisonerno and c.department in (select departmentid from departmentpos where posid ='" + posID + "')";
            string prisonerno, prisonername, Abalance, Bbalance, Zbalance, permitinfo, prisonerHead;
            int iprisonerno = 0;
            string[] limit;
            string tmpLimit = "", tmpString1, tmpHead = "", tmpL;
            string num;
            bool firstFlag = true;
            int retValue = 1, tmpInt;
            string retVale = "2";

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            SqlCommand cmd = new SqlCommand("proc_shopping_limit_data_load_by_posid", conn);

            try
            {
                conn.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@posid", SqlDbType.Char, 10);
                cmd.Parameters["@posid"].Value = posID;

                cmd.Parameters.Add("@retVale", SqlDbType.Char, 1);
                cmd.Parameters["@retVale"].Value = retVale;
                cmd.Parameters["@retVale"].Direction = System.Data.ParameterDirection.Output;


                cmd.ExecuteNonQuery();

                retVale = cmd.Parameters["@retVale"].Value.ToString();
                if (!retVale.Equals("0"))
                {
                    Log.writelog("执行存储过程proc_shopping_limit_data_load_by_posid失败！\t\n");
                    return retValue;
                }

                cmd.CommandText = sel_sql;
                cmd.CommandType = CommandType.Text;
                SqlDataReader dr = cmd.ExecuteReader();


                FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("gb2312"));

                while (dr.Read())
                {
                    num = dr["num"].ToString().Trim().PadRight(5, ' ');
                    prisonerHead = dr["prisonerno"].ToString().Trim().Substring(0, 4);

                    //if (prisonerHead.Equals("3309"))
                    //{// 应监狱要求 加密犯人编码  

                    //    iprisonerno = int.Parse(dr["prisonerno"].ToString().Trim().Substring(3));
                    //    iprisonerno = iprisonerno * 3 - 1298543;
                    //    prisonerno = iprisonerno.ToString().PadRight(20, ' ');

                    //}
                    //else
                    //{
                    //    if (prisonerHead.Equals("3340"))
                    //    {
                    //        iprisonerno = int.Parse("8" + dr["prisonerno"].ToString().Trim().Substring(4));
                    //        iprisonerno = iprisonerno * 3 - 1298543;
                    //        prisonerno = iprisonerno.ToString().PadRight(20, ' ');
                    //    }
                    //    else
                    //    {
                    //        prisonerno = dr["prisonerno"].ToString().Trim().PadRight(20, ' ');
                    //    }
                    //}
                    prisonerno = dr["prisonerno"].ToString().Trim().PadRight(20, ' ');

                    prisonername = dr["prisonername"].ToString().Trim().PadRight(10, ' ');

                    prisonername = convertstring(prisonername, 10);

                    Abalance = dr["Abalance"].ToString().Trim().PadRight(10, ' ');
                    Bbalance = dr["Bbalance"].ToString().Trim().PadRight(10, ' ');
                    Zbalance = dr["Zbalance"].ToString().Trim().PadRight(10, ' ');

                    //0|A|1|327.57#3|A|1|50.00#3|C|1|120.00#4|A|3|0.00  
                    permitinfo = dr["permitinfo"].ToString().Trim();
                    limit = permitinfo.Split('#');
                    tmpLimit = "";
                    for (int j = 0; j < limit.Length; j++)
                    {
                        tmpL = limit[j].Substring(0, limit[j].LastIndexOf('.'));
                        tmpString1 = tmpL.Replace("|", "").PadRight(7, ' ');
                        tmpLimit = tmpLimit + tmpString1;

                    }

                    tmpString1 = "";
                    if (firstFlag)
                    {
                        tmpString1 = prisonerno + prisonername + Abalance + Bbalance + Zbalance + tmpLimit;
                        tmpInt = GetUTF8Length(tmpString1);
                        tmpHead = num + "U" + tmpInt.ToString().PadRight(3) + tmpLimit.Length.ToString().PadRight(3);
                        tmpString1 = tmpHead + tmpString1;
                        firstFlag = false;
                    }
                    else
                    {
                        tmpString1 = prisonerno + prisonername + Abalance + Bbalance + Zbalance + tmpLimit;
                    }

                    sw.Write(tmpString1);

                }

                dr.Close();


                sw.Close();

                fs.Close();

                retValue = 0;

            }
            catch (Exception ex)
            {
                Log.SetException(ex);

            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return retValue;

        }

        private string convertstring(string preString, int length)
        {
            string tempbuf = "";

            Encoding utf8 = Encoding.GetEncoding(65001);
            Encoding gb2312 = Encoding.GetEncoding("gb2312");
            byte[] temp = utf8.GetBytes(preString);
            byte[] flbyte = Encoding.Convert(utf8, gb2312, temp);
            tempbuf = gb2312.GetString(flbyte, 0, length);

            return tempbuf;

        }

        private int GetUTF8Length(string gb2321String)
        {
            return Encoding.GetEncoding("gb2312").GetByteCount(gb2321String);
        }

        /// <summary>
        /// 获取商品列表
        /// </summary>
        private int GetGoodsFile(string posID, string filePath)
        {

            string sel_deptime = " select top 1 department,datetime from solutions "
                               + " where department =(select top 1 substring(rtrim(ltrim(  departmentid)),1,2)+'00' as posdep  from departmentpos where posid = '" + posID.Trim() + "')   and inuse = '1' ";

            /*string sel_sql = " select a.goodid,a.goodname,a.spec,a.type,a.unit,a.price,"
                           + " (select count(goodid) from solutiondetail where department='{0}' and datetime='{1}'  ) as num "
                           + " from goodslist a,solutiondetail b  where a.goodid = b.goodid and b.department='{0}' and b.datetime='{1}' ";*/

            string sel_sql = "select goodid,goodname,spec,type,unit,price,(select count(*) from goodslist) as num from goodslist";
                          

            string sel_type = " select id+typename as type from goodstype where isdel = '0' ";

            string department = "", datetime = "", tmpSql = "";

            string goodid, goodname, spec, type, unit, price, num;
            string tmpString1 = "", typeString = "", tmpBody = "";
            byte[] tmpbyte = new byte[30];
            bool firstFlag = true;
            int retValue = 1;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }


            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sel_type, conn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    tmpString1 = dr.GetString(0).Trim().PadRight(11, ' ');
                    typeString = typeString + convertstring(tmpString1, 11);
                }

                dr.Close();

                /*cmd.CommandText = sel_deptime;
                dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    department = dr.GetString(0).Trim();
                    datetime = dr.GetString(1).Trim();
                }
                else
                {
                    Log.writelog("pos:" + posID + "的商品方案没有配置！\t\n");
                    return retValue;
                }

                dr.Close();*/

                //tmpSql = string.Format(sel_sql, department, datetime);
                cmd.CommandText = sel_sql;
                dr = cmd.ExecuteReader();

                FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("gb2312"));

                while (dr.Read())
                {
                    num = dr["num"].ToString().Trim().PadRight(5, ' ');
                    goodid = dr["goodid"].ToString().Trim().PadRight(4, ' ');
                    goodname = dr["goodname"].ToString().Trim().PadRight(30, ' ');
                    goodname = convertstring(goodname, 30);

                    spec = dr["spec"].ToString().Trim().PadRight(10, ' ');
                    spec = convertstring(spec, 10);

                    type = dr["type"].ToString().Trim();
                    unit = dr["unit"].ToString().Trim().PadRight(8, ' ');
                    unit = convertstring(unit, 8);

                    price = dr["price"].ToString().Trim().PadRight(10, ' ');

                    tmpString1 = "";
                    if (firstFlag)
                    {
                        tmpBody = goodid + goodname + spec + unit + price + type;
                        tmpString1 = num + "S" + GetUTF8Length(tmpBody).ToString().PadRight(3) + GetUTF8Length(typeString).ToString().PadRight(3) + typeString + tmpBody;

                        firstFlag = false;
                    }
                    else
                    {
                        tmpString1 = goodid + goodname + spec + unit + price + type;
                    }

                    sw.Write(tmpString1);

                }

                dr.Close();

                sw.Flush();
                sw.Close();

                fs.Close();

                retValue = 0;
            }
            catch (Exception ex)
            {
                Log.SetException(ex);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            return retValue;

        }

        /// <summary>
        /// 接收订单字符流，保存文件后，写入数据库
        /// </summary>
        /// <param name="sb"></param>
        /// <returns></returns>
        public int GetOrderFile(StringBuilder sb)
        {
            string tmpstring, tmpPrisonerno;
            int retValue = 1, bodyLen, num, iPrisonerno;

            string orderdate, ordertime, prisonerno, goodid, price, quantity, amout, type;
            IList arrayList = new ArrayList();

            string filePath = ConfigurationManager.AppSettings["filePath"].ToString().Trim() + "data" + DateTime.Now.ToString("yyMMddHHmmss") + ".txt";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, Encoding.Unicode);

            //条数
            num = Int32.Parse(sb.ToString().Substring(0, 5).Trim());
            bodyLen = Int32.Parse(sb.ToString().Substring(6, 3).Trim()); //63; 
            sb = sb.Remove(0, 9);

            int index;
            for (index = 0; index < num; index++)
            {
                tmpstring = sb.ToString().Substring(0, bodyLen);
                sw.WriteLine(tmpstring);
                arrayList.Add(tmpstring);

                sb = sb.Remove(0, bodyLen);
            }

            sw.Close();
            fs.Close();

            //写入数据库
            string tmpsql;
            string ins_sql = " insert into prisonerorder( orderdate,ordertime,prisonerno,goodid,price,quantity,amout,type)"
                           + "                    values( '{0}','{1}','{2}','{3}',{4},{5},{6},'{7}')";
            SqlCommand cmd = new SqlCommand();

            conn.Open();
            SqlTransaction sqlTransaction = conn.BeginTransaction();

            try
            {
                cmd.Transaction = sqlTransaction;
                cmd.Connection = conn;

                for (index = 0; index < arrayList.Count; index++)
                {

                    tmpstring = arrayList[index].ToString();

                    orderdate = tmpstring.Substring(0, 8).Trim();
                    ordertime = tmpstring.Substring(8, 6).Trim();
                    prisonerno = tmpstring.Substring(14, 20).Trim();
                    goodid = tmpstring.Substring(34, 4).Trim();
                    price = tmpstring.Substring(38, 10).Trim();
                    quantity = tmpstring.Substring(48, 4).Trim();
                    amout = tmpstring.Substring(52, 10).Trim();
                    type = tmpstring.Substring(62, 1).Trim();

                    //if (prisonerno.Length == 8)
                    //{
                    //    iPrisonerno = int.Parse(prisonerno);
                    //    tmpPrisonerno = Convert.ToString((iPrisonerno + 1298543) / 3);
                    //    if (tmpPrisonerno.StartsWith("9"))
                    //    {
                    //        prisonerno = "330" + tmpPrisonerno;
                    //    }

                    //    if (tmpPrisonerno.StartsWith("8"))
                    //    {
                    //        prisonerno = "3340" + tmpPrisonerno.Substring(1);
                    //    }

                    //}

                    tmpsql = String.Format(ins_sql, orderdate, ordertime, prisonerno, goodid, price, quantity, amout, type);
                    Log.SetString(tmpsql);
                    cmd.CommandText = tmpsql;
                    cmd.ExecuteNonQuery();

                }

                sqlTransaction.Commit();

                retValue = 0;
            }
            catch (Exception ex)
            {
                sqlTransaction.Rollback();
                Log.SetException(ex);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }

            }

            return retValue;
        }

    }
}