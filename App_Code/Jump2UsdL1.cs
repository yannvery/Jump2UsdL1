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
    UsdObject usdObject;
    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


    public Jump2UsdL1()
    {

        this.usdObject = new UsdObject();    
    }

    public void initializeResource(string methodName)
    {
        this.methodName = methodName;
        try
        {
            this.myUsdService = new USD_WebService();
            this.myWsTools = new wsTools();
        }
        catch (Exception e)
        {
            this.usdObject.ReturnCode = "ERR";
            this.usdObject.ReturnMessage = e.Message;
            this.usdObject.Description = "Can't initialize resource";
            this.usdObject.Success = false;
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
            setReturn("Log in failed", "-1", e.Message, false);
        }
        return sid;
    }

    public void setReturn(string description, string code, string message, bool success)
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
        this.usdObject.ReturnCode = code;
        this.usdObject.ReturnMessage = message;
        this.usdObject.Description = description;
        this.usdObject.Success = success;
    }

    public Boolean validatePresence(string param_name, string param_value)
    {
        Boolean myReturn = true;
        if (param_value == null)
        {
            setReturn("Invalid parameter", "1", "parameter " + param_name + " is null", false);
            myReturn = false;
        }
        return myReturn;
    }

    public Boolean validatePresenceNotEmpty(string param_name, string param_value)
    {
        Boolean myReturn = true;
        if (String.IsNullOrEmpty(param_value))
        {
            setReturn("Invalid parameter", "2", "parameter " + param_name + " is null or empty", false);
            myReturn = false;
        }
        return myReturn;
    }

    [WebMethod]
    public UsdObject attachEvent(string username, string password, string name, string crpersid)
    {
        initializeResource("attachEvent");
        string myResult;
        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - username : " + username + " - password : " + password + " - name : " + name);

        // Validate parameters
        if (!(validatePresenceNotEmpty("username", username))) return this.usdObject;
        if (!(validatePresence("password", password))) return this.usdObject;
        if (!(validatePresenceNotEmpty("name", name))) return this.usdObject;
        if (!(validatePresenceNotEmpty("crpersid", crpersid))) return this.usdObject;

        this.mySID = login(username, password);
        if (this.mySID == 0)
        {
            return this.usdObject;
        }
        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - SID : " + this.mySID);

        //Get Handle
        try
        {
            myResult = this.myUsdService.doSelect(this.mySID, "evt", "sym = '" + name + "'", -1, this.myHandleAttr);
            XmlDocument myValue = new XmlDocument();
            myValue.LoadXml(myResult);
            myHandle = myValue.SelectSingleNode("//UDSObject/Handle/text()").Value;
        }
        catch (Exception e)
        {
            setReturn("Can't find event", "-1", e.Message, false);
            return this.usdObject;
        }
        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - Handle : " + myHandle);
        
        // create object
         string[] myAttrVals = {
                "obj_id",
                crpersid,
                "event_tmpl",
                myHandle,
            };
        string[] myAttributes = {
                "persistent_id"
            };
        string createObjectReturn = "";
        string newHandle= "";
        try
        {
            this.myUsdService.createObject(this.mySID, "atev", myAttrVals, myAttributes, ref createObjectReturn, ref newHandle);
            setReturn("Attach event done", "0", newHandle, true);
        }
        catch (Exception e)
        {
            setReturn("Can't attach event", "-1", e.Message, false);
            return this.usdObject;
        }
        // logout
        if (this.mySID != 0)
        {
            this.myUsdService.logout(this.mySID);
        }
        return this.usdObject;
    }

    [WebMethod]
    public UsdObject jumpAcknowledge(string username, string password, string ref_num, string jump_num)
    {
        this.methodName = "jumpAcknowledge";
        initializeResource(methodName);
        string myResult;
        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - username : " + username + " - password : " + password + " - ref_num : " + ref_num + " - jump_num : " + jump_num);

        // Validate parameters
        if (!(validatePresenceNotEmpty("username", username))) return this.usdObject;
        if (!(validatePresence("password", password))) return this.usdObject;
        if (!(validatePresenceNotEmpty("ref_num", ref_num))) return this.usdObject;
        if (!(validatePresenceNotEmpty("jump_num", jump_num))) return this.usdObject;

        DateTime saveNow = DateTime.Now;
        TimeSpan elapsedTime = saveNow - Epoch;

        this.mySID = login(username, password);
        if (this.mySID == 0)
        {
            return this.usdObject;
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
            setReturn("Can't find incident", "3", e.Message, false);
            return this.usdObject;
        }
        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - Handle : " + myHandle);
        
        //Update CR
        try
        {
            int totalsec = (int)elapsedTime.TotalSeconds;
            //myResult = this.myUsdService.updateObject(this.mySID, myHandle, new string[] { "zjump", jump_num, "zjump_majdt", totalsec.ToString() }, new string[0]);
            myResult = this.myUsdService.updateObject(this.mySID, myHandle, new string[] { "zjump", jump_num, }, new string[0]);
            setReturn("Acknowledge creation of request", "0", "", true);
        }
        catch (Exception e)
        {
            setReturn("Incident can't be updated", "4", e.Message, false);
            return this.usdObject;
        }

        this.myWsTools.log("INFO", this.methodName + " - " + this.mySID + " - Incident updated");
        
        // logout
        if (this.mySID != 0)
        {
            this.myUsdService.logout(this.mySID);
        }

        return this.usdObject;
    }

}
