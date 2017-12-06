# GnatMQ

![](images/gnat.jpg)

GnatMQ - MQTT Broker for .NET and WinRT

## Description

A broker (server) for the MQTT protocol, an M2M Internet-of-Things communication protocol based on .Net Framework. 

MQTT, short for Message Queue Telemetry Transport, is a light weight messaging protocol that enables embedded devices with limited resources to perform asynchronous communication on a constrained network.

Developed by IBM and Eurotech, the MQTT protocol is released as an open standard and being standardized by OASIS (Organization for the Advancement of Structured Information Standard), a non-profit consortium that drives the development, convergence and adoption of open standards for the global information society.

In general, the MQTT environment consists of multiple clients and a server, called broker.

This project is created to develop an MQTT broker.  While there are other MQTT broker project, this project is created to provide additional resources to learn and engage in developing useful resources for the MQTT protocol, in conjunction with the M2Mqtt project, a .NET client library for MQTT that supports .NET Framework, .NET Compact Framework and .NET Micro Framework.

## How to use:
```
Install-Package GnatMQ_Broker -Version 1.2.0
```
Starting the server is simple:
```C#
using uPLibrary.Networking.M2Mqtt;

static void Main(string[] args)
{
    // create and start broker
    MqttBroker broker = new MqttBroker();
    broker.Start();
	//Once the broker is started, you applciaiton is free to do whatever it wants. 
    Console.ReadLine();
	
	///Stop broker
    broker.Stop();
}
```
The broker can also be embedded in an applicaiton, 
be that a cloud server, desktop app or even a UWP applicaiton. 

## Supported Platforms: 
* .Net Framework (up to 4.5)
* .Net Compact Framework 3.5 & 3.9 (for Windows Embedded Compact 7 / 2013)
* .Net Micro Framework 4.2 & 4.3
* Mono (for Linux O.S.)
* Windows 8.1
* Windows Phone 8.1
* Windows 10 (Through .Net Standard 2)
* .Net Core (Through .Net Standard 2)

## Features

**Main features included in the current release:**

* All three Quality of Service (QoS) Levels (at most once, at least once, exactly once);
* Clean session;
* Retained messages;
* Will message (QoS, topic and message);
* Username/Password via a User Access Control;
* Subscription to topics with wildcards;
* Publish and subscribe handle using inflight queue;
* Security connection with SSL/TLS;

**Features not included in the current release:**

* Broker configuration using a simple config file;
* Bridge configuration (broker to broker);
* Sessions, retained and will messages persisted at broker shutdown (ex. database); 

## Contributing 
Contributions are welcome. Please submit a PR against the Dev branch. 
Because the software supprots so many platforms, testing it is a little involved. 
Because GnatMQ supports .Net Compact Framework 3.9, building nuget packages requires VS2015 Pro or Enterprise and  
Applicaiton Builder for [Compact Framework, download here](https://www.microsoft.com/en-us/download/details.aspx?id=38819). 


## More Information
* The project has an official website here :  https://m2mqtt.wordpress.com/
* or more information about MQTT, visit: http://www.mqtt.org
* For more information about OASIS, visit: https://www.oasis-open.org
* There is an MQTT client, M2Mqtt released as community resource on this GitHub repo : https://github.com/ppatierno/m2mqtt