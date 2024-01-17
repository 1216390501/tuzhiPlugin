using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicObject = Kingdee.BOS.Orm.DataEntity.DynamicObject;

namespace changePaperAbstractPlugin
{
    [Description("更新各表图纸")]
    [HotUpdate]
    public class Class1PaperAbstractPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("F_VPRD_material");
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {

            base.AfterExecuteOperationTransaction(e);
            foreach (ExtendedDataEntity extended in e.SelectedRows)
            {
                DynamicObject dy = extended.DataEntity;
                DynamicObject material = dy["F_VPRD_material"] as DynamicObject;
                string materialNumber= material["Number"].ToString();//物料编码
                string materName = materialNumber + "V-";
                string maxVersionSQL = string.Format(@"/*dialect*/ SELECT TOP 1 FNUMBER,FID FROM VPRD_t_Cust100012 WHERE FNUMBER LIKE '{0}%' ORDER BY CAST(REPLACE(FNUMBER, '{0}', '') AS INT) DESC", materName);
                var dataset = DBUtils.ExecuteDynamicObject(this.Context, maxVersionSQL);//取到最新的图纸

                if (dataset != null && dataset.Count > 0)
                {
                    string number = dataset[0]["FNUMBER"].ToString();
                    var fid = dataset[0]["FID"];

                    //生产订单
                    string strSql = string.Format(@"/*dialect*/ select a.FId from T_PRD_MOENTRY_A a Inner Join T_PRD_MOENTRY b on a.FENTRYID = b.FENTRYID where FStatus in ('1','2','3','4') and b.FMATERIALID={0}", material["Id"]);
                    //string strSql = string.Format(@"/*dialect*/ select * from T_PRD_MOENTRY_A a Inner Join T_PRD_MOENTRY b on a.FENTRYID = b.FENTRYID where FStatus in ('1','2','3','4') and b.FMATERIALID={0}", 101513);
                    var dynObjs = DBUtils.ExecuteDynamicObject(this.Context, strSql);
                    var arrPks = dynObjs.Select(x => x["FId"]).ToArray();
                    var bInfo = FormMetaDataCache.GetCachedFormMetaData(this.Context, "PRD_MO").BusinessInfo;
                    foreach (var billPk in arrPks)
                    {
                        DynamicObject dynamicObject = BusinessDataServiceHelper.LoadSingle(this.Context, billPk, bInfo.GetDynamicObjectType());
                        DynamicObjectCollection entry = dynamicObject["TreeEntity"] as DynamicObjectCollection;
                        for (int i = 0; i < entry.Count(); i++)
                        {
                            DynamicObject mater = entry[i]["MaterialId"] as DynamicObject;
                            string status = entry[i]["Status"].ToString();

                            if (mater != null && IsValidStatus(status))
                            {
                                if (mater["Number"].Equals(material["Number"])) {
                                    entry[i]["F_VPRD_paper_Id"] = fid;
                                }
                            }
                            var result = BusinessDataServiceHelper.Save(this.Context, dynamicObject);
                        }
                    }
                    //采购订单
                    string strSql2 = string.Format(@"/*dialect*/ select main.FId from t_PUR_POOrderEntry entry inner join t_PUR_POOrder main on main.fid=entry.fid where main.FCLOSESTATUS='A' and entry.FMATERIALID={0}", material["Id"]);
                    var dynObjs2 = DBUtils.ExecuteDynamicObject(this.Context, strSql);
                    var arrPks2 = dynObjs2.Select(x => x["FId"]).ToArray();
                    var bInfo2 = FormMetaDataCache.GetCachedFormMetaData(this.Context, "PUR_PurchaseOrder").BusinessInfo;
                    foreach (var billPk in arrPks2)
                    {
                        DynamicObject dynamicObject = BusinessDataServiceHelper.LoadSingle(this.Context, billPk, bInfo2.GetDynamicObjectType());
                        DynamicObjectCollection entry = dynamicObject["POOrderEntry"] as DynamicObjectCollection;
                        for (int i = 0; i < entry.Count(); i++)
                        {
                            DynamicObject mater = entry[i]["MaterialId"] as DynamicObject;
                            if (mater != null)
                            {
                                if (mater["Number"].Equals(material["Number"]))
                                {
                                    entry[i]["F_VPRD_paper_Id"] = fid;
                                }
                            }
                            var result = BusinessDataServiceHelper.Save(this.Context, dynamicObject);
                        }
                    }
                    //销售订单
                    string strSql3 = string.Format(@"/*dialect*/ select main.FId from T_SAL_ORDERENTRY entry inner join T_SAL_ORDER main on entry.fid=main.fid where main.FCLOSESTATUS='A' AND entry.FMATERIALID={0}", material["Id"]);
                    var dynObjs3 = DBUtils.ExecuteDynamicObject(this.Context, strSql);
                    var arrPks3 = dynObjs3.Select(x => x["FId"]).ToArray();
                    var bInfo3 = FormMetaDataCache.GetCachedFormMetaData(this.Context, "SAL_SaleOrder").BusinessInfo;
                    foreach (var billPk in arrPks3)
                    {
                        DynamicObject dynamicObject = BusinessDataServiceHelper.LoadSingle(this.Context, billPk, bInfo3.GetDynamicObjectType());
                        DynamicObjectCollection entry = dynamicObject["SaleOrderEntry"] as DynamicObjectCollection;
                        for (int i = 0; i < entry.Count(); i++)
                        {
                            DynamicObject mater = entry[i]["MaterialId"] as DynamicObject;
                            if (mater != null)
                            {
                                if (mater["Number"].Equals(material["Number"]))
                                {
                                    entry[i]["F_VPRD_paper_Id"] = fid;
                                }
                            }
                            var result = BusinessDataServiceHelper.Save(this.Context, dynamicObject);
                        }
                    }
                }
            }
        }
        public static bool IsValidStatus(string status)
        {
            switch (status)
            {
                case "1":
                case "2":
                case "3":
                case "4":
                    return true;
                default:
                    return false;
            }
        }
    }
}
