
// SampleClient.cs:


using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
namespace TestCode.Remoting
{
  public class SampleClient
  {
    public static void Main(string [] args)
    {
       // Create a channel for communicating w/ the remote object
       // Notice no port is specified on the client
      HttpChannel channel = new HttpChannel();
      ChannelServices.RegisterChannel(channel);
           
      // Create an instance of the remote object
      SampleObject obj = (SampleObject) Activator.GetObject( 
        typeof(TestCode.Remoting.SampleObject),
        "http://localhost:8080/GetCount" );
      // Use the object
      if( obj.Equals(null) )
      {
        System.Console.WriteLine("Error: unable to locate server");
      }
      else
      {
        Console.WriteLine("counter: {0}", obj.GetCount());
      }
    } 
  }
}




