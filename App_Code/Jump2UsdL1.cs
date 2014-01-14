using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Services;
using System.Xml;

using USD_WS;
/// <summary>
/// Summary description for Jump2UsdL1
/// </summary>
[WebService(Namespace = "http://localhost/jump_ws/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
// [System.Web.Script.Services.ScriptService]
public class Jump2UsdL1 : System.Web.Services.WebService
{
    private USD_WebService myUsdService;
    private wsTools myWsTools;
    private int mySID = 0;
    private string myHandle = null;
    private string methodName = null;
    private string[] myHandleAttr = { "persistent_id" };
    private XmlDocument docObject;
    private XmlNode XmlReturn;
    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


    public Jump2UsdL1()
    {

        this.docObject = new XmlDocument();     
    }

    public void initializeResource(string methodName)
    {
        this.docObject.LoadXml("<" + methodName + "Response><" + methodName + "Result></" + methodName + "Result></" + methodName + "Response>");
        this.XmlReturn = this.docObject.SelectNodes("/" + methodName + "Response/" + methodName + "Result")[0];

        try
        {
            this.myUsdService = new USD_WebService();
            this.myWsTools = new wsTools();
        }
        catch (Exception e)
        {
            this.myWsTools.CreateXmlNode("ReturnCode", "ERR", this.docObject, this.XmlReturn);
            this.myWsTools.CreateXmlNode("ReturnMessage", e.Message, this.docObject, this.XmlReturn);
        }
    }

    public int login(string username, string password)
    {
        int sid = 0;
        try
        {
            sid = this.myUsdService.login(username, password);
        }
        catch (Exception e)
        {
            setXmlReturn("Log in failed", "-1", e.Message, "false");
        }
        return sid;
    }

    public void setXmlReturn(string description, string code, string message, string success)
    {
        string logMessage = "";
        string logDescription = "";
        string level = "ERROR";
        if (code == "0")
        {
            level = "INFO";
        }
        if (!(String.IsNullOrEmpty(message)))
        {
            logMessage = " - ReturnMessage : " + message;
        }
        if (!(String.IsNullOrEmpty(description)))
        {
            logDescription = " - Description : " + description;
        }
        this.myWsTools.log(level, this.methodName + " - " + this.mySID + logDescription + logMessage);
        this.myWsTools.CreateXmlNode("Description", description, this.docObject, this.XmlReturn);
        this.myWsTools.CreateXmlNode("ReturnCode", code, this.docObject, this.XmlReturn);
        this.myWsTools.CreateXmlNode("ReturnMessage", message, this.docObject, this.XmlReturn);
        this.myWsTools.CreateXmlNode("success", success, this.docObject, this.XmlReturn);
    }

    public Boolean validatePresence(string param_name, string param_value)
    {
        Boolean myReturn = true;
        if (param_value == null)
        {
            setXmlReturn("Invalid parameter", "1", "parameter " + param_name + " is null", "false");
            myReturn = false;
        }
        return myReturn;
    }

    public Boolean validatePresenceNotEmpty(string param_name, string param_value)
    {
        Boolean myReturn = true;
        if (String.IsNullOrEmpty(param_value))
        {
            setXmlReturn("Invalid parameter", "2", "parameter " + param_name + " is null or empty", "false");
            myReturn = false;
        }
        return myReturn;
    }

    [WebMethod]
    public XmlNode jumpAcknowledge(string username, string password, string ref_num, string jump_num)
    {
        this.methodName = "jumpAcknowledge";
        initializeResource(methodName);
        string myResult;
        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - username : " + username + " - password : " + password + " - ref_num : " + ref_num + " - jump_num : " + jump_num);

        // Validate parameters
        if (!(validatePresenceNotEmpty("username", username))) return this.docObject;
        if (!(validatePresence("password", password))) return this.docObject;
        if (!(validatePresenceNotEmpty("ref_num", ref_num))) return this.docObject;
        if (!(validatePresenceNotEmpty("jump_num", jump_num))) return this.docObject;

        DateTime saveNow = DateTime.Now;
        TimeSpan elapsedTime = saveNow - Epoch;

        this.mySID = login(username, password);
        if (this.mySID == 0)
        {
            return this.docObject;
        }
        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - SID : " + this.mySID);
        
        //Get Handle
        try
        {
            myResult = this.myUsdService.doSelect(this.mySID, "cr", "ref_num = '" + ref_num + "' AND active = 1", -1, this.myHandleAttr);
            XmlDocument myValue = new XmlDocument();
            myValue.LoadXml(myResult);
            myHandle = myValue.SelectSingleNode("//UDSObject/Handle/text()").Value;
        }
        catch (Exception e)
        {
            setXmlReturn("Can't find incident", "3", e.Message, "false");
            return this.docObject;
        }
        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - Handle : " + myHandle);
        
        //Update CR
        try
        {
            myResult = this.myUsdService.updateObject(this.mySID, myHandle, new string[] { "zjump", jump_num, "zjump_majdt", elapsedTime.TotalSeconds.ToString() }, new string[0]);
            setXmlReturn("Acknowledge creation of request", "0", "", "true");
        }
        catch (Exception e)
        {
            setXmlReturn("Incident can't be updated", "4", e.Message, "false");
            return this.docObject;
        }

        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - Incident updated");
        // logout
        if (this.mySID != 0)
        {
            this.myUsdService.logout(this.mySID);
        }

        return this.docObject;
    }
}
