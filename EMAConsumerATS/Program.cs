namespace EMAConsumerATS;

using System;
using System.Threading;
using LSEG.Ema.Access;

public class AppClient : IOmmConsumerClient
{
    public void OnRefreshMsg(RefreshMsg refreshMsg, IOmmConsumerEvent consumerEvent)
    {
        Console.WriteLine(refreshMsg);
    }

    public void OnUpdateMsg(UpdateMsg updateMsg, IOmmConsumerEvent consumerEvent)
    {
        Console.WriteLine(updateMsg);
    }

    public void OnStatusMsg(StatusMsg statusMsg, IOmmConsumerEvent consumerEvent)
    {
        Console.WriteLine(statusMsg);
    }
}

class Program
{
    static void Main()
    {
        OmmConsumer? consumer = null;
        try
        {
            AppClient appClient = new();
            OmmConsumerConfig config = new OmmConsumerConfig().ConsumerName("Consumer_ATS").UserName("user");
            consumer = new OmmConsumer(config);
            consumer.RegisterClient(new RequestMsg().ServiceName("ELEKTRON_DD").Name("THB="), appClient);
            Thread.Sleep(60000); // API calls OnRefreshMsg(), OnUpdateMsg() and OnStatusMsg()
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
}
