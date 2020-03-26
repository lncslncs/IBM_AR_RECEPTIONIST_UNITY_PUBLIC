#r "Newtonsoft.Json"
#r "System.Runtime"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Configuration;
using System.Text.RegularExpressions;
using System;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

static string requestBody;          //HTTP body as JSON
static dynamic body;                //requestBody deserialised
static int requestIndex;            //code for what type of database request to make, and what to do with the output
static string requestType;          //denotes table for SQL query
static string[] valid_key_names;    //List of keys to cycle through when constructing query
static string accountSid;
static string authToken;            //2 codes for connecting to Twilio

public static async Task<IActionResult> Run(HttpRequest req, ILogger log, ExecutionContext context)
{
    log.LogInformation("C# HTTP trigger function processed a request.");//all logs are for debugging and seeing what requests come in
    dealWithVariables(req);         
    log.LogInformation(requestBody);

    string result = "";
    switch(requestIndex){//3 example businesses kept separate
        case 0:
        case 1:
        case 2:
        case 3:
        case 4:
            result = visitingOrganisation(log);
            break;
        case 5:
        case 6:
        case 7:
        case 8:
        case 9:
            result = library();
            break;
        case 10:
        case 11:
        case 12:
            result = GPSurgery(log);
            break;
        default:
            result = "invalid requestType";
            break;
    }
    log.LogInformation("Result = " + result);
    if(string.Equals(result, "")){
        return (ActionResult)new OkObjectResult(body);
    }//no result from database request -> return the original body

    string[] lines = Regex.Split(result, "\n");
    var dictionary = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(requestBody);
    int i = 0;
    JToken token = body["Multiple"];//used to denote multiple lines to one key

    foreach(var key in dictionary.Keys){//Fill in the blanks in requestBody with the Database results
        if (i >= lines.Length){
            break;
        }
        if (string.Equals(dictionary[key].ToString(), "?") && valid_key_names.Contains(key)){
            if(token != null){
                body[key] = result;
            }
            else{
                body[key] = lines[i];
            }
            i++;
        }
    }

    return (ActionResult)new OkObjectResult(body);

}

public static string databaseRequest(string query){//makes connection, sends request and then adds each line of the result to the return variable
    string toReturn = "";
    string connectionInfo = Environment.GetEnvironmentVariable("sqldb_connection");
    using (SqlConnection connection = new SqlConnection(connectionInfo)){
        connection.Open();
        SqlCommand command = new SqlCommand(query, connection);
        using(SqlDataReader reader = command.ExecuteReader()){
            int count = reader.FieldCount;
            while(reader.Read()){
                for (int i = 0; i < count; i++){
                    toReturn = toReturn + reader.GetValue(i).ToString() + "\n";
                }
            }
            return toReturn;
        }
        
    }
    return "Could not be found";
}


public static String makeQuery(bool forcePhoneNumber, string columnToForce){
    String query = "SELECT ";
    var dictionary = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(requestBody);

    if(!forcePhoneNumber){//Adds all blank keys after select
        foreach(var key in dictionary.Keys){
            if (string.Equals(dictionary[key].ToString(), "?") && valid_key_names.Contains(key)){
                query = query + (key.ToString()).Replace("'", "''") + ", ";
            }
        }
        if (string.Equals(query, "SELECT ")){//nothing blank -> select all
            query = query + "* ";
        }
        else{
            query = query.Substring(0, query.Length-2) + " ";//remove superfluous ', '
        }
        query = query + "FROM " + requestType + " WHERE ";
    }

    else{//option to force a single column, such as phone number, to notify staff without unnecessarily sharing their details
        query = query + columnToForce + " FROM " + requestType + " WHERE ";
    }
    foreach(var key in dictionary.Keys){//Adds conditions
        if ((!(string.Equals(dictionary[key].ToString(), "?"))) && valid_key_names.Contains(key)){
            query = query + key + " = '" + (dictionary[key].ToString()).Replace("'", "''") + "' AND ";
        }
    }
    query = query.Substring(0, query.Length-5) + ";";
    return query;
}


public static string allEventsOnDay(){
    String query = "SELECT * FROM events WHERE Date = '" + body["Date"] + "';";
    return query;
}


public static string joinWithStaffOnKey(String key, string tableA){
    return (tableA + " INNER JOIN staff ON " + tableA + ".StaffID = staff.StaffID");
}


public static async void dealWithVariables(HttpRequest req){
    requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    body = JsonConvert.DeserializeObject(requestBody);//HTTP request body as JObject to be easier to deal with
    requestIndex = body["requestType"];
    requestType = Regex.Split(Environment.GetEnvironmentVariable("request_types"), ", ")[requestIndex];//environment variable which has the table for each request type
    accountSid = Environment.GetEnvironmentVariable("accountSid");
    authToken = Environment.GetEnvironmentVariable("authToken");
}


public static void testTwilio(string toNumber, string textContent){//sends Twilio text
    TwilioClient.Init(accountSid, authToken);
    var message = MessageResource.Create(
            body: textContent,
            from: new Twilio.Types.PhoneNumber("+18326264397"),
            to: new Twilio.Types.PhoneNumber(toNumber)
        );

        Console.WriteLine(message.Sid);
}

public static string visitingOrganisation(ILogger log){
    string result = "";
    switch(requestIndex){
        case 0:
        //get information about staff, such as room number given a name
        case 1:
        //get information about events
            string envVirKey = "valid_key_names_" + requestIndex.ToString();
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable(envVirKey), ", ");
            // log.LogInformation(makeQuery(false, ""));
            // log.LogInformation(requestType);//********
            result = databaseRequest(makeQuery(false, ""));
            if(string.Equals(result, "")){
                result = "Could not be found";
            }
            log.LogInformation(result);
            break;
        case 2:
        //List times of events on a day
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable("valid_key_names_2"), ", ");
            result = timesOfEvents();// gets human readable list of times, such as "09:00, 10:00 and 11:00"
            if(string.Equals(result, "")){
                result = "there are no events happening here today";
            }
            break;
        case 3:
        //Similar to 0, but sends a text to the staff member
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable("valid_key_names_0"), ", ");
            result = databaseRequest(makeQuery(false, ""));
            string number = databaseRequest(makeQuery(true, "PhoneNumber"));
            if (!string.Equals(number.Trim(), "")){
                testTwilio(number, "There is someone at reception to see you.");
            }
            if(string.Equals(result.Trim(), "")){
                result = "Could not be found";
            }
            break;
        case 4:
            //retrieves information about the organisation
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable("valid_key_names_3"), ", ");
            result = databaseRequest(OrganisationInfoQuery());
            if(string.Equals(result, "")){
                result = "Could not be found";
            }
            break;
        default:
            result = "Could not be found";
            break;
    }
    return result;
}

public static string library(){
    string result = "";
    switch(requestIndex){
        case 5:
        //Times of events for library
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable("valid_key_names_2"), ", ");
            result = timesOfEvents();
            if(string.Equals(result, "")){
                result = "there are no events happening here today";
            }
            break;
        case 6:
        //retrieve events information for library
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable("valid_key_names_1"), ", ");
            result = databaseRequest(makeQuery(false, ""));
            if(string.Equals(result, "")){
                result = "Could not be found";
            }
            break;
        case 7:
        //Organisation info for library
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable("valid_key_names_3"), ", ");
            result = databaseRequest(OrganisationInfoQuery());
            if(string.Equals(result, "")){
                result = "Could not be found";
            }
            break;
        case 8:
        //Get information about books in the library
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable("valid_key_names_4"), ", ");
            result = databaseRequest(makeQuery(false, ""));
            if(string.Equals(result, "")){
                result = "Could not be found";
            }
            break;
        default:
            break;
    }
    return result;
}

public static string GPSurgery(ILogger log){
    string result = "";
    switch(requestIndex){
        case 10:
        //Have an appointment, text the doctor
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable("valid_key_names_5"), ", ");
            requestType = "((gp_appointments INNER JOIN gp_patients ON gp_appointments.PatientID = gp_patients.PatientID) INNER JOIN gp_doctors ON gp_appointments.DoctorID = gp_doctors.DoctorID)";
            string query = makeQuery(false, "");//hardcoded join table for now
            databaseRequest(SetQuery("gp_appointments", "CheckedIn", "1"));
            result = databaseRequest(query);
            string number = databaseRequest(makeQuery(true, "DoctorPhone"));
            if(!string.Equals(number, "")){
                testTwilio(number, "Your patient is in reception");
            }
            string[] list = Regex.Split(result, "\n");
            string temp = "";
            for (int i = 0; i < list.Length; i++){
                if (string.Equals(list[i], "True")){
                    list[i] = "you have been checked in for your appointment";
                }
                else if (string.Equals(list[i], "False")){
                    list[i] = "There has been an error checking you in";
                }
                temp = temp + list[i] + "\n";
            }
            result = temp;
            if (string.IsNullOrEmpty(result.Trim())){
                result = "There has been an error checking you in";
            }
            
            break;//3 database requests - check patient in, get requested information and get phone number to text doctor
        case 11:
        //pick up prescription
            valid_key_names = Regex.Split(Environment.GetEnvironmentVariable("valid_key_names_6"), ", ");
            requestType = "(gp_prescriptions INNER JOIN gp_patients ON gp_prescriptions.PatientID = gp_patients.PatientID) INNER JOIN gp_doctors ON gp_prescriptions.DoctorID = gp_doctors.DoctorID";
            result = databaseRequest(makeQuery(false, ""));
            databaseRequest(SetQuery("gp_prescriptions", "HasBeenDelivered", "1"));//similar to 10, gets relevant information then marks the prescription as delivered
            if(string.Equals(result, "")){
                result = "Prescription could not be found";
            }

            list = Regex.Split(result, "\n");
            temp = "";
            for (int i = 0; i < list.Length; i++){
                if (string.Equals(list[i], "True")){
                    list[i] = "has been delivered";
                }
                else if (string.Equals(list[i], "False")){
                    list[i] = "hasn't been delivered";
                }
                temp = temp + list[i] + "\n";
            }
            result = temp;

            break;
        case 12:
        //General request for human assistance for tasks not forseen
            testTwilio(Environment.GetEnvironmentVariable("default_number"), "A reception user is requesting a human to help them.");
            break;
        default:
            break;
    }
    return result;
}

public static string timesOfEvents(){
    string date = body["Date"];
    if (string.IsNullOrEmpty(date)){
        date = DateTime.Today.ToString("dd/MM/yyyy");//Date given or today's date
    }
    string query = "SELECT Time FROM " + requestType + " WHERE Date = '" + date + "';";
    string result = databaseRequest(query);
    string toReturn = "";
    string[] list = Regex.Split(result, "\n");
    int length = list.Length;
    for(int i = 0; i < length-1; i++){
        if (i == length - 2 && length > 2){
            toReturn = toReturn + "and " + list[i];
        }
        else if (i == length - 2){
            toReturn = list[i];
        }
        else{
            toReturn = toReturn + list[i] + ", ";
        }
    }//toReturn -> human readable list of times, if there are multiple
    if (length == 1){
        return result;
    }
    return toReturn;
}

public static string OrganisationInfoQuery(){//slightly different format to get organisation info
    string query = "SELECT InfoText FROM " + requestType + " WHERE ";
    var dictionary = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(requestBody);
    foreach(var key in dictionary.Keys){
            if (string.Equals(dictionary[key].ToString(), "?") && valid_key_names.Contains(key)){
                query = query + "InfoType = '" + key + "' OR ";
            }
    }
    return query.Substring(0, query.Length - 4);
}

public static string SetQuery(string table, string column, string value){//updates value, such as checked in for appointment
    string query = "UPDATE " + table +" SET " + column + " = '" + value + "' FROM " + requestType +  " WHERE ";
    var dictionary = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(requestBody);
    foreach(var key in dictionary.Keys){
        if ((!(string.Equals(dictionary[key].ToString(), "?"))) && valid_key_names.Contains(key)){
            query = query + key + " = '" + (dictionary[key].ToString()).Replace("'", "''") + "' AND ";
        }
    }
    query = query.Substring(0, query.Length-5) + ";";
    return query;
}
