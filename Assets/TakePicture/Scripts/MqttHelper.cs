﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Internal;
using System.Net.Security;
using System.Net.Sockets;
using TMPro;

public class MqttHelper : MonoBehaviour
{
    private MqttClient client;

    // The connection information
    public string brokerHostname = "ec2-11-111-11.us-west-2.compute.amazonaws.com";
    public int brokerPort = 8883;
    public string userName = "test";
    public string password = "test";
    public TextAsset certificate;
    public TextMeshPro info;
    private string infotext = "waiting for message";

    // listen on all the Topic
   // static string subTopic = "#";
    static string subTopic = "tohololens";
    // Start is called before the first frame update
    void Start()
    {
        if (brokerHostname != null && userName != null && password != null)
        {
            Debug.Log("connecting to " + brokerHostname + ":" + brokerPort);
            Connect();
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE };
            client.Subscribe(new string[] { subTopic }, qosLevels);
        }

    }

    // Update is called once per frame
    void Update()
    {
        info.SetText(infotext);


    }
    private void Connect()
    {
        Debug.Log("about to connect on '" + brokerHostname + "'");
        // Forming a certificate based on a TextAsset
        //X509Certificate cert = new X509Certificate();
        //cert.Import(certificate.bytes);
        //Debug.Log("Using the certificate '" + cert + "'");

        //client = new MqttClient(brokerHostname, brokerPort, true, cert, null, MqttSslProtocols.TLSv1_0, MyRemoteCertificateValidationCallback);
        client = new MqttClient(brokerHostname, brokerPort, false, null, null, MqttSslProtocols.None);

        string clientId = System.Guid.NewGuid().ToString();
        Debug.Log("About to connect using '" + userName + "' / '" + password + "'");
        try
        {
            client.Connect(clientId, userName, password);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Connection error: " + e);
        }
    }
    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string msg = System.Text.Encoding.UTF8.GetString(e.Message);
        Debug.Log("Received message from " + e.Topic + " : " + msg);
        infotext =  "Received message from " + e.Topic + " : " + msg;
        
        
    }
    public void Publish(string _topic, string msg)
    {
        if (client.IsConnected == true)
        {
            client.Publish(
                _topic, System.Text.Encoding.UTF8.GetBytes(msg),
                MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        } else
        {
            Debug.LogError("cannot publish, client not connected");
        }
    }
}
