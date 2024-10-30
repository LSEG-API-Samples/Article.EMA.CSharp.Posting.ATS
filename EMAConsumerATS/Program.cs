///*|----------------------------------------------------------------------------------------------------
// *|            This source code is provided under the Apache 2.0 license
// *|  and is provided AS IS with no warranty or guarantee of fit for purpose.
// *|                See the project's LICENSE.md for details.
// *|           Copyright (C) 2024 LSEG. All rights reserved.     
///*|----------------------------------------------------------------------------------------------------

namespace EMAConsumerATS;


using LSEG.Ema.Access;
using System.Threading;
using System;
using static LSEG.Ema.Access.DataType;
using LSEG.Ema.Rdm;
using System.Net;
using System.Net.Sockets;
using System.Linq;

internal class AppClient : IOmmConsumerClient
{
    public string PostServiceName { get; set; } = "ATS";
    public string PostItemName { get; set; } = "ATS_RIC";

    public long UserId { get; set; }
    public long UserAddress { get; set; }
    public string ATSAction { get; set; } = "Update";

    private static int postId = 1;

    /// <summary>
    /// Handle incoming Refresh Messages from the backend
    /// </summary>
    /// <param name="refreshMsg">Refresh Msg obj</param>
    /// <param name="consumerEvent">incoming OMM Consumer Event obj</param>
    public void OnRefreshMsg(RefreshMsg refreshMsg, IOmmConsumerEvent consumerEvent)
    {
        Console.WriteLine($"Received Refresh. Item Handle: {consumerEvent.Handle} Closure: {(consumerEvent.Closure ?? "null")}");

        Console.WriteLine($"Item Name: {(refreshMsg.HasName ? refreshMsg.Name() : "<not set>")}");
        Console.WriteLine($"Service Name: {(refreshMsg.HasServiceName ? refreshMsg.ServiceName() : "<not set>")}");

        Console.WriteLine($"Item State: {refreshMsg.State()}");

        // Receive a Login refreh response message, ready for OffStream Posting
        if (refreshMsg.DomainType() == EmaRdm.MMT_LOGIN &&
                refreshMsg.State().StreamState == OmmState.StreamStates.OPEN &&
                refreshMsg.State().DataState == OmmState.DataStates.OK)
        {
            // Choose ATS Action
            switch (ATSAction)
            {
                case "create": //Send OMM Post to create a new RIC on ATS
                    CreateRIC(consumerEvent);
                    break;
                case "update": //Send OMM Post to update RIC prices on ATS
                    UpdateRIC(consumerEvent); 
                    break;
                case "addfields": //Send OMM Post to add fields of RIC on ATS
                    AddFields(consumerEvent); 
                    break;
                case "removefields": //Send OMM Post to remove fields of RIC on ATS
                    DeleteFields(consumerEvent);
                    break;
                case "delete": //Send OMM Post to remove a RIC on ATS
                    DeleteRIC(consumerEvent);
                    break;
                default:
                    Console.WriteLine("Wrong command. Support \"create\", \"addfields\", \"removefields\", \"delete\", \"update\"  only");
                    break;
            }
        }

        Decode(refreshMsg);

        Console.WriteLine();
    }

    public void OnUpdateMsg(UpdateMsg updateMsg, IOmmConsumerEvent consumerEvent)
    {
        Console.WriteLine($"Received Update. Item Handle: {consumerEvent.Handle} Closure: {(consumerEvent.Closure ?? "null")}");

        Console.WriteLine($"Item Name: {(updateMsg.HasName ? updateMsg.Name() : "<not set>")}");
        Console.WriteLine($"Service Name: {(updateMsg.HasServiceName ? updateMsg.ServiceName() : "<not set>")}");

        Decode(updateMsg);

        Console.WriteLine();
    }

    /// <summary>
    /// Handle incoming Status Messages from the backend
    /// </summary>
    /// <param name="statusMsg">Status Msg object</param>
    /// <param name="consumerEvent">incoming OMM Consumer Event obj</param>
    public void OnStatusMsg(StatusMsg statusMsg, IOmmConsumerEvent consumerEvent)
    {
        Console.WriteLine($"Received Status. Item Handle: {consumerEvent.Handle} Closure: {(consumerEvent.Closure ?? "null")}");

        Console.WriteLine($"Item Name: {(statusMsg.HasName ? statusMsg.Name() : "<not set>")}");
        Console.WriteLine($"Service Name: {(statusMsg.HasServiceName ? statusMsg.ServiceName() : "<not set>")}");

        if (statusMsg.HasState)
            Console.WriteLine("Item State: " + statusMsg.State());

        Console.WriteLine();
    }

    /// <summary>
    /// Handle incoming Ack Messages from the backend
    /// </summary>
    /// <param name="ackMsg">Ack Msg object</param>
    /// <param name="consumerEvent">incoming OMM Consumer Event obj</param>
    public void OnAckMsg(AckMsg ackMsg, IOmmConsumerEvent consumerEvent)
    {
        Console.WriteLine($"Received AckMsg. Item Handle: {consumerEvent.Handle} Closure: {(consumerEvent.Closure ?? "null")}");

        //Decode(ackMsg);
        Console.WriteLine($"AckId: {ackMsg.AckId()}");
        Console.WriteLine($"NackCode: {(ackMsg.HasNackCode ? ackMsg.NackCode() : "<not set>")}");
        Console.WriteLine($"Text: {(ackMsg.HasText ? ackMsg.Text() : "<not set>")}");

        Console.WriteLine();
    }


    private void Decode(Msg msg)
    {
        switch (msg.Attrib().DataType)
        {
            case DataTypes.ELEMENT_LIST:
                Decode(msg.Attrib().ElementList());
                break;

            case DataTypes.FIELD_LIST:
                Decode(msg.Attrib().FieldList());
                break;

            default:
                break;
        }

        switch (msg.Payload().DataType)
        {
            case DataTypes.ELEMENT_LIST:
                Decode(msg.Payload().ElementList());
                break;

            case DataTypes.FIELD_LIST:
                Decode(msg.Payload().FieldList());
                break;

            default:
                break;
        }
    }

    private void Decode(ElementList elementList)
    {
        foreach (ElementEntry elementEntry in elementList)
        {
            Console.Write($" Name = {elementEntry.Name} DataType: {DataType.AsString(elementEntry.Load!.DataType)} Value: ");

            if (Data.DataCode.BLANK == elementEntry.Code)
                Console.WriteLine(" blank");
            else
                switch (elementEntry.LoadType)
                {
                    case DataTypes.REAL:
                        Console.WriteLine(elementEntry.OmmRealValue().AsDouble());
                        break;

                    case DataTypes.DATE:
                        Console.WriteLine($"{elementEntry.OmmDateValue().Day}/{elementEntry.OmmDateValue().Month}/{elementEntry.OmmDateValue().Year}");
                        break;

                    case DataTypes.TIME:
                        Console.WriteLine($"{elementEntry.OmmTimeValue().Hour}:{elementEntry.OmmTimeValue().Minute}:{elementEntry.OmmTimeValue().Second}:{elementEntry.OmmTimeValue().Millisecond}");
                        break;

                    case DataTypes.INT:
                        Console.WriteLine(elementEntry.IntValue());
                        break;

                    case DataTypes.UINT:
                        Console.WriteLine(elementEntry.UIntValue());
                        break;

                    case DataTypes.ASCII:
                        Console.WriteLine(elementEntry.OmmAsciiValue());
                        break;

                    case DataTypes.ENUM:
                        Console.WriteLine(elementEntry.EnumValue());
                        break;

                    case DataTypes.RMTES:
                        Console.WriteLine(elementEntry.OmmRmtesValue());
                        break;

                    case DataTypes.ERROR:
                        Console.WriteLine($"{elementEntry.OmmErrorValue().ErrorCode} ({elementEntry.OmmErrorValue().ErrorCodeAsString()})");
                        break;

                    default:
                        Console.WriteLine();
                        break;
                }
        }
    }

    private void Decode(FieldList fieldList)
    {
        foreach (FieldEntry fieldEntry in fieldList)
        {
            Console.Write($"Fid: {fieldEntry.FieldId} Name = {fieldEntry.Name} DataType: {DataType.AsString(fieldEntry.Load!.DataType)} Value: ");

            if (Data.DataCode.BLANK == fieldEntry.Code)
            {

                Console.WriteLine(" blank");
            }
            else
                switch (fieldEntry.LoadType)
                {
                    case DataTypes.REAL:
                        Console.WriteLine(fieldEntry.OmmRealValue().AsDouble());
                        break;

                    case DataTypes.DATE:
                        Console.WriteLine($"{fieldEntry.OmmDateValue().Day} / {fieldEntry.OmmDateValue().Month} / {fieldEntry.OmmDateValue().Year}");
                        break;

                    case DataTypes.TIME:
                        Console.WriteLine($"{fieldEntry.OmmTimeValue().Hour}:{fieldEntry.OmmTimeValue().Minute}:{fieldEntry.OmmTimeValue().Second}:{fieldEntry.OmmTimeValue().Millisecond}");
                        break;

                    case DataTypes.INT:
                        Console.WriteLine(fieldEntry.IntValue());
                        break;

                    case DataTypes.UINT:
                        Console.WriteLine(fieldEntry.UIntValue());
                        break;

                    case DataTypes.ASCII:
                        Console.WriteLine(fieldEntry.OmmAsciiValue());
                        break;

                    case DataTypes.ENUM:
                        Console.WriteLine(fieldEntry.HasEnumDisplay ? fieldEntry.EnumDisplay() : fieldEntry.EnumValue());
                        break;

                    case DataTypes.RMTES:
                        Console.WriteLine(fieldEntry.OmmRmtesValue());
                        break;

                    case DataTypes.ERROR:
                        Console.WriteLine($"{fieldEntry.OmmErrorValue().ErrorCode}({fieldEntry.OmmErrorValue().ErrorCodeAsString()})");
                        break;

                    default:
                        Console.WriteLine();
                        break;
                }
        }
    }

    /// <summary>
    /// Send the Post Msg to create a new RIC on ATS
    /// </summary>
    /// <param name="consumerEvent">incoming OMM Consumer Event obj</param>
    private void CreateRIC(IOmmConsumerEvent consumerEvent) 
    {
        try
        {
            Console.WriteLine("Create ATS New RIC");
            PostMsg postMsg = new();
            RefreshMsg nestedRefreshMsg = new();
            FieldList nestedFieldList = new();

            //FieldList is a collection
            nestedFieldList.AddAscii(-1, PostItemName);
            nestedFieldList.AddReal(22, 12, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
            nestedFieldList.AddReal(25, 13, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
            nestedFieldList.AddTime(5, 11, 29, 30);
            nestedFieldList.Complete();
            nestedRefreshMsg.Payload(nestedFieldList).Complete(true);


            ((OmmConsumer)consumerEvent!.Closure!).Submit(postMsg.PostId(postId++).ServiceName(PostServiceName)
                                                        .Name("ATS_INSERT_S").SolicitAck(true).Complete(true).PublisherId(UserId, UserAddress)
                                                        .Payload(nestedRefreshMsg), consumerEvent.Handle);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
       
        
    }

    /// <summary>
    /// Send the Post Msg to update RIC values on ATS
    /// </summary>
    /// <param name="consumerEvent">incoming OMM Consumer Event obj</param>
    private void UpdateRIC(IOmmConsumerEvent consumerEvent)
    {
        try
        {
            Console.WriteLine("Update ATS RIC");
            PostMsg postMsg = new();
            UpdateMsg nestedUpdateMsg = new();
            FieldList nestedFieldList = new();

            //FieldList is a collection
            nestedFieldList.AddReal(22, 43, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
            nestedFieldList.AddReal(25, 44, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
            nestedFieldList.AddTime(5, 11, 30, 30);
            nestedFieldList.Complete();
            nestedUpdateMsg.Payload(nestedFieldList);
            ((OmmConsumer)consumerEvent!.Closure!).Submit(postMsg.PostId(postId++).ServiceName(PostServiceName)
                                                        .Name(PostItemName).SolicitAck(true).Complete(true).PublisherId(UserId, UserAddress)
                                                        .Payload(nestedUpdateMsg), consumerEvent.Handle);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
    }

    /// <summary>
    /// Send the Post Msg to add new fields of RIC on ATS
    /// </summary>
    /// <param name="consumerEvent">incoming OMM Consumer Event obj</param>
    private void AddFields(IOmmConsumerEvent consumerEvent)
    {
        try
        {
            Console.WriteLine("Add ATS Fields");
            PostMsg postMsg = new();
            UpdateMsg nestedUpdateMsg = new();
            FieldList nestedFieldList = new();

            //FieldList is a collection
            nestedFieldList.AddAscii(-1, PostItemName);
            nestedFieldList.AddReal(12, 22, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
            nestedFieldList.AddReal(13, 3, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
            nestedFieldList.Complete();
            nestedUpdateMsg.Payload(nestedFieldList);
            ((OmmConsumer)consumerEvent!.Closure!).Submit(postMsg.PostId(postId++).ServiceName(PostServiceName)
                                                        .Name("ATS_ADDFIELD_S").SolicitAck(true).Complete(true).PublisherId(UserId, UserAddress)
                                                        .Payload(nestedUpdateMsg), consumerEvent.Handle);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
       
    }

    /// <summary>
    /// Send the Post Msg to delete fields of RIC on ATS
    /// </summary>
    /// <param name="consumerEvent">incoming OMM Consumer Event obj</param>
    private void DeleteFields(IOmmConsumerEvent consumerEvent)
    {
        try
        {
            Console.WriteLine("Delete ATS Fields");
            PostMsg postMsg = new();
            UpdateMsg nestedUpdateMsg = new();
            FieldList nestedFieldList = new();

            //FieldList is a collection
            nestedFieldList.AddAscii(-1, PostItemName);
            nestedFieldList.AddReal(12, 1, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
            nestedFieldList.AddReal(13, 2, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
            nestedFieldList.Complete();
            nestedUpdateMsg.Payload(nestedFieldList);
            ((OmmConsumer)consumerEvent!.Closure!).Submit(postMsg.PostId(postId++).ServiceName(PostServiceName)
                                                        .Name("ATS_DELETE").SolicitAck(true).Complete(true).PublisherId(UserId, UserAddress)
                                                        .Payload(nestedUpdateMsg), consumerEvent.Handle);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
    }

    /// <summary>
    /// Send the Post Msg to remove RIC on ATS
    /// </summary>
    /// <param name="consumerEvent">incoming OMM Consumer Event obj</param>
    private void DeleteRIC(IOmmConsumerEvent consumerEvent)
    {
        try
        {
            Console.WriteLine("Delete ATS RIC");
            PostMsg postMsg = new();
            UpdateMsg nestedUpdateMsg = new();
            FieldList nestedFieldList = new();

            //FieldList is a collection
            nestedFieldList.AddAscii(-1, PostItemName);
            nestedFieldList.Complete();
            nestedUpdateMsg.Payload(nestedFieldList);
            ((OmmConsumer)consumerEvent!.Closure!).Submit(postMsg.PostId(postId++).ServiceName(PostServiceName)
                                                        .Name("ATS_DELETE_ALL").SolicitAck(true).Complete(true).PublisherId(UserId, UserAddress)
                                                        .Payload(nestedUpdateMsg), consumerEvent.Handle);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
    }
}

class Program
{
    static string ServiceName = "ATS";
    static string SubItem = "RIC_NAME";
    static string PostItem = "CREATED.RIC";
    static string DACSUserName = "USER";
    static string ATSAction = "update";
    static long DACSUserID = Environment.ProcessId;
    static void Main(string[] args)
    {
        OmmConsumer? consumer = null;
        try
        {
            // Get IP Address as string
            string ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();
            
            // Get HostName
            string hostName = Dns.GetHostName();
            // Read input command line arguments
            ReadCommandlineArgs(args);
            // Init AppClient class
            AppClient appClient = new();
            // Pass input data to Appclient class
            appClient.PostServiceName =ServiceName;
            appClient.PostItemName = PostItem;
            appClient.UserId = DACSUserID;
            // Get IP Address as Long
            appClient.UserAddress = BitConverter.ToInt32(IPAddress.Parse(ipAddress).GetAddressBytes(), 0);
            appClient.ATSAction = ATSAction;

            // Establish the RSSL connection to ADS Server
            OmmConsumerConfig config = new OmmConsumerConfig().ConsumerName("Consumer_ATS").UserName(DACSUserName).Position($"{ipAddress}/{hostName}");
            consumer = new OmmConsumer(config);
            // Register Login Domain 
            RequestMsg requestMsg = new();
            Console.WriteLine("Consumer: Sending Login Domain Request message");
            consumer.RegisterClient(requestMsg.DomainType(EmaRdm.MMT_LOGIN), appClient, consumer);

            //Console.WriteLine("Consumer: Sending Item Request message");
            //consumer.RegisterClient(new RequestMsg().ServiceName(ServiceName).Name(SubItem), appClient,consumer);

            Thread.Sleep(600000); // API calls OnRefreshMsg(), OnUpdateMsg() and OnStatusMsg()
        }
        catch (OmmException excp)
        {
            Console.WriteLine(excp.Message);
        }
        finally
        {
            consumer?.Uninitialize();
        }
    }

    /// <summary>
    /// Read incoming commandline arguements
    /// </summary>
    /// <param name="args">commandline arguements</param>
    static void ReadCommandlineArgs(string[] args)
    {
        string[] actions = { "create", "addfields", "removefields", "delete", "update" };

        try
        {
            int argsCount = 0;
            while (argsCount < args.Length)
            {
                if (0 == args[argsCount].CompareTo("-H"))
                {
                    printHelp();
                    Environment.Exit(0);
                }
                else if ("-service".Equals(args[argsCount]))
                {
                    ServiceName = argsCount < (args.Length - 1) ? args[++argsCount] : "ATS";
                    ++argsCount;
                }
                else if ("-user".Equals(args[argsCount]))
                {
                    DACSUserName = argsCount < (args.Length - 1) ? args[++argsCount] : "USER";
                    ++argsCount;
                }
                else if ("-item".Equals(args[argsCount]))
                {
                    PostItem = argsCount < (args.Length - 1) ? args[++argsCount] : "CREATED.RIC";
                    ++argsCount;
                }
                else if ("-action".Equals(args[argsCount]))
                {
                    if (argsCount < (args.Length - 1)) ATSAction = args[++argsCount];
                    ++argsCount;
                    if(!actions.Contains(ATSAction))
                    {
                        printHelp();
                        Environment.Exit(0);
                    }
                }
                else // unrecognized command line argument
                {
                    printHelp();
                    Environment.Exit(0);
                }
            }
        }
        catch
        {
            printHelp();
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Printing application help message on a console
    /// </summary>
    static void printHelp()
    {
        Console.WriteLine("\nOptions:\n" + "  -H\tShows this usage\n"
                + "  -service ADS service name that connects to ATS server\n"
                + "  -user DACS Username (if uses DACS)\n"
                + "  -item RIC name to interact with ATS\n"
                + "  -action ATS Action {create, addfields, removefields, delete, update}\n"
                + "\n");
    }
}
