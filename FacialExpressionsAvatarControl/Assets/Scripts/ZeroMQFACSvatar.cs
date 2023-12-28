using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using NetMQ;
using UnityEngine;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

public enum Participants
{
    users_1_models_1, users_1_models_2, users_1_models_2_dnn,
    users_2_models_2, users_2_models_2_dnn, users_2_models_3_dnn
}

public class NetMqListener
{
    public string sub_to_ip;
    public string sub_to_port;
    public bool facsvatar_logging = false;
    private readonly Thread _listenerWorker;
    private bool _listenerCancelled;
    public delegate void MessageDelegate(List<string> msg_list);
    private readonly MessageDelegate _messageDelegate;
    private readonly ConcurrentQueue<List<string>> _messageQueue = new ConcurrentQueue<List<string>>();
	  private string csv_path = "Assets/Logging/unity_timestamps_sub.csv";
	  private StreamWriter csv_writer;
	  private long msg_count;
    public NetMqListener(string sub_to_ip, string sub_to_port) {
        this.sub_to_ip = sub_to_ip;
        this.sub_to_port = sub_to_port;
    }

    private void ListenerWork()
    {
		    Debug.Log("Setting up subscriber sock");
        AsyncIO.ForceDotNet.Force();
        using (var subSocket = new SubscriberSocket())
        {
            subSocket.Options.ReceiveHighWatermark = 1000;
            subSocket.Connect("tcp://"+sub_to_ip+":"+sub_to_port);
            subSocket.Subscribe("");
            Debug.Log("sub socket initiliased");

            string topic;
            string timestamp;
            string facsvatar_json;
            while (!_listenerCancelled)
            {

                List<string> msg_list = new List<string>();
                if (!subSocket.TryReceiveFrameString(out topic)) continue;
                if (!subSocket.TryReceiveFrameString(out timestamp)) continue;
                if (!subSocket.TryReceiveFrameString(out facsvatar_json)) continue;

                if (timestamp != "")
                {
                    msg_list.Add(topic);
					          msg_list.Add(timestamp);
                    msg_list.Add(facsvatar_json);
                    long timeNowMs = UnixTimeNowMillisec();
                    msg_list.Add(timeNowMs.ToString()); 
                    
                    if (facsvatar_logging == true)
                    {
					              string csvLine = string.Format("{0},{1}", msg_count, timeNowMs);
					              csv_writer.WriteLine(csvLine);
					          }
					          msg_count++;
					          
                    _messageQueue.Enqueue(msg_list);
                }
                // done
                else
                {
                    Debug.Log("Received all messages");
                }
            }
            subSocket.Close();
        }
        NetMQConfig.Cleanup();
    }

	public static long UnixTimeNowMillisec()
    {
        DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        long unixTimeStampInTicks = (DateTime.UtcNow - unixStart).Ticks;
		    long timeNowMs = unixTimeStampInTicks / (TimeSpan.TicksPerMillisecond / 10000); 
        return timeNowMs;
    }

    public void Update()
    {
        while (!_messageQueue.IsEmpty)
        {
            List<string> msg_list;
            if (_messageQueue.TryDequeue(out msg_list))
            {
                _messageDelegate(msg_list);
            }
            else
            {
                break;
            }
        }
    }

    public NetMqListener(MessageDelegate messageDelegate)
    {
        _messageDelegate = messageDelegate;
        _listenerWorker = new Thread(ListenerWork);
    }

    public void Start()
    {
        if (facsvatar_logging == true)
        {
		        Debug.Log("Setting up Logging NetMqListener");
		        msg_count = -1;
		        File.Delete(csv_path); 
		        csv_writer = new StreamWriter(csv_path, true); 
            csv_writer.WriteLine("msg,time_prev,time_now");
		        csv_writer.Flush();
        }

        _listenerCancelled = false;
        _listenerWorker.Start();
    }

    public void Stop()
    {
        _listenerCancelled = true;
        _listenerWorker.Join();
        if (facsvatar_logging == true)
        {
		        csv_writer.Close();
		    }
    }
}

public class ZeroMQFACSvatar : MonoBehaviour
{
    private NetMqListener _netMqListener;
    public string sub_to_ip = "127.0.0.1";
    public string sub_to_port = "5579";
    public bool facsvatar_logging = false;

    public Participants participants;

	  private long msg_count;
	  private string csv_folder = "Assets/Logging/";
	  private string csv_path = "Assets/Logging/unity_timestamps_sub.csv";
	  private StreamWriter csv_writer;   
	  private string csv_path_total = "Assets/Logging/unity_timestamps_total.csv";
    private StreamWriter csv_writer_total;

    public FACSnimator FACSModel0;
	public HeadRotatorBone RiggedModel0;
	public FACSnimator FACSModel1;
	public HeadRotatorBone RiggedModel1;
    public FACSnimator FACSModelDnn;
	public HeadRotatorBone RiggedModelDnn;
    public FACSnimator FACSModel2;
    public HeadRotatorBone RiggedModel2;

    private string userIgnoreString = "p1";

    private void HandleMessage(List<string> msg_list)
    {
        JObject facsvatar = JObject.Parse(msg_list[2]);
        // get Blend Shape dict
        JObject blend_shapes = facsvatar["blendshapes"].ToObject<JObject>();
		    // get head pose data
		    JObject head_pose = facsvatar["pose"].ToObject<JObject>();

		    string[] topic_info = msg_list[0].Split('.');

        Debug.Log(participants);

        if (participants == Participants.users_1_models_1 & Array.IndexOf(topic_info, "dnn") == -1)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(FACSModel0.RequestBlendshapes(blend_shapes));
            UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel0.RequestHeadRotation(head_pose));
        }

        else if (participants == Participants.users_1_models_2 & Array.IndexOf(topic_info, "dnn") == -1)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(FACSModel0.RequestBlendshapes(blend_shapes));
            UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel0.RequestHeadRotation(head_pose));
            UnityMainThreadDispatcher.Instance().Enqueue(FACSModel1.RequestBlendshapes(blend_shapes));
            UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel1.RequestHeadRotation(head_pose));
            UnityMainThreadDispatcher.Instance().Enqueue(FACSModel2.RequestBlendshapes(blend_shapes));
            UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel2.RequestHeadRotation(head_pose));
        }

        else if (participants == Participants.users_1_models_2_dnn)
        {
            if (Array.IndexOf(topic_info, "dnn") != -1)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(FACSModelDnn.RequestBlendshapes(blend_shapes));
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModelDnn.RequestHeadRotation(head_pose));
            }
            else
            {
                UnityMainThreadDispatcher.Instance().Enqueue(FACSModel0.RequestBlendshapes(blend_shapes));
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel0.RequestHeadRotation(head_pose));
            }
        }

        else if (participants == Participants.users_2_models_2 & Array.IndexOf(topic_info, "dnn") == -1)
        {
            if (Array.IndexOf(topic_info, "p0") != -1)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(FACSModel0.RequestBlendshapes(blend_shapes));
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel0.RequestHeadRotation(head_pose));
            }
            else if (Array.IndexOf(topic_info, "p1") != -1)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(FACSModel1.RequestBlendshapes(blend_shapes));
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel1.RequestHeadRotation(head_pose));
            }
        }

        else if (participants == Participants.users_2_models_2_dnn)
        {

            if (Array.IndexOf(topic_info, "dnn") != -1)
            {
                userIgnoreString = facsvatar["user_ignore"].ToString();

                if (userIgnoreString == "p0")
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(FACSModel0.RequestBlendshapes(blend_shapes));
                }
                else if (userIgnoreString == "p1")
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(FACSModel1.RequestBlendshapes(blend_shapes));
                }
            }

            else if (Array.IndexOf(topic_info, "p0") != -1)
            {
                if (userIgnoreString != "p0")
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(FACSModel0.RequestBlendshapes(blend_shapes));
                }
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel0.RequestHeadRotation(head_pose));
            }

            else if (Array.IndexOf(topic_info, "p1") != -1)
            {
                if (userIgnoreString != "p1")
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(FACSModel1.RequestBlendshapes(blend_shapes));
                }
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel1.RequestHeadRotation(head_pose));
            }

            else if (Array.IndexOf(topic_info, "p2") != -1)
            {
                if (userIgnoreString != "p2")
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(FACSModel2.RequestBlendshapes(blend_shapes));
                }
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel2.RequestHeadRotation(head_pose));
            }
        }

        else if (participants == Participants.users_2_models_3_dnn)
        {
            if (Array.IndexOf(topic_info, "dnn") != -1)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(FACSModelDnn.RequestBlendshapes(blend_shapes));
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModelDnn.RequestHeadRotation(head_pose));
            }
            else if (Array.IndexOf(topic_info, "p0") != -1)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(FACSModel0.RequestBlendshapes(blend_shapes));
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel0.RequestHeadRotation(head_pose));
            }
            else if (Array.IndexOf(topic_info, "p1") != -1)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(FACSModel1.RequestBlendshapes(blend_shapes));
                UnityMainThreadDispatcher.Instance().Enqueue(RiggedModel1.RequestHeadRotation(head_pose));
            }
        }

    if (facsvatar_logging == true)
    {
		    long timeNowMs = UnixTimeNowMillisec();
		    long timestampMsgArrived = Convert.ToInt64(msg_list[3]);
		    string csvLine = string.Format("{0},{1},{2}", msg_count, timestampMsgArrived, timeNowMs);
        csv_writer.WriteLine(csvLine);

		    if (facsvatar["timestamp_utc"] != null)
		    {
			    long timeFirstSend = Convert.ToInt64(facsvatar["timestamp_utc"].ToString());

			    string csvLine_total = string.Format("{0},{1},{2}", msg_count, timeFirstSend, timeNowMs);
          csv_writer_total.WriteLine(csvLine_total);
		    }
		}

		msg_count++;
    }

	public static long UnixTimeNowMillisec()
    {
        DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        long unixTimeStampInTicks = (DateTime.UtcNow - unixStart).Ticks;
        long timeNowMs = unixTimeStampInTicks / (TimeSpan.TicksPerMillisecond / 10000);  
        return timeNowMs;
    }

    private void Start()
    {
        if (facsvatar_logging == true)
        {
		        Debug.Log("Setting up Logging ZeroMQFACSvatar");
            msg_count = -1;

            Directory.CreateDirectory(csv_folder);
            File.Delete(csv_path);  
            csv_writer = new StreamWriter(csv_path, true); 
            csv_writer.WriteLine("msg,time_prev,time_now");
            csv_writer.Flush();

		        File.Delete(csv_path_total); 
		        csv_writer_total = new StreamWriter(csv_path_total, true); 
		        csv_writer_total.WriteLine("msg,time_prev,time_now");
		        csv_writer_total.Flush();
		    }

        _netMqListener = new NetMqListener(HandleMessage);
        _netMqListener.sub_to_ip = sub_to_ip;
        _netMqListener.sub_to_port = sub_to_port;
        _netMqListener.Start();
    }

    private void StartOnNewIP()
    {
        _netMqListener = new NetMqListener(HandleMessage);
        _netMqListener.sub_to_ip = sub_to_ip;
        _netMqListener.sub_to_port = sub_to_port;
        _netMqListener.Start();
    }
    
    public string DisplayName
    {
        get
        {
            string fullDisplayName = FACSModel0.ToString();
            int underscoreIndex = fullDisplayName.IndexOf('_');
        
            if (underscoreIndex != -1)
            {
                return fullDisplayName.Substring(0, underscoreIndex);
            }

            return fullDisplayName;
        }
    }

    private void Update()
    {
        if (_netMqListener.sub_to_ip == sub_to_ip && _netMqListener.sub_to_port == sub_to_port)
            _netMqListener.Update();
        else StartOnNewIP();
    }

    private void OnDestroy()
    {
        _netMqListener.Stop();
    }
}
