// High level script for connecting AR Receiptionist to IBM Services
// UCL COMP0016 Team 12 Jan 2020; Written by Lilly Neubauer
// Adapted from https://github.com/IBM/Watson-Unity-ARKit
// Sections that were added and not taken from above code are marked "NEW"

#define ENABLE_DEBUGGING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IBM.Watson.Assistant.V2;
using IBM.Watson.Assistant.V2.Model;
using IBM.Watson.SpeechToText.V1;
using IBM.Watson.TextToSpeech.V1;
using IBM.Cloud.SDK;
using IBM.Cloud.SDK.Authentication;
using IBM.Cloud.SDK.Authentication.Iam;
using IBM.Cloud.SDK.Connection;
using IBM.Cloud.SDK.Utilities;
using IBM.Cloud.SDK.DataTypes;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;




public class NewWatson : MonoBehaviour
{
    
    #region PLEASE SET THESE VARIABLES IN THE INSPECTOR
    [Header("Watson Assistant")]
    [Tooltip("The service URL (optional). This defaults to \"https://gateway.watsonplatform.net/assistant/api\"")]
    [SerializeField]
    private string AssistantURL;
    [SerializeField]
    private string assistantId;
    [Tooltip("The apikey.")]
    [SerializeField]
    private string assistantIamApikey;

    [Header("Speech to Text")]
    [Tooltip("The service URL (optional). This defaults to \"https://stream.watsonplatform.net/speech-to-text/api\"")]
    [SerializeField]
    private string SpeechToTextURL;
    [Tooltip("The apikey.")]
    [SerializeField]
    private string SpeechToTextIamApikey;

    [Header("Text to Speech")]
    [SerializeField]
    [Tooltip("The service URL (optional). This defaults to \"https://stream.watsonplatform.net/text-to-speech/api\"")]
    private string TextToSpeechURL;
    [Tooltip("The apikey.")]
    [SerializeField]
    private string TextToSpeechIamApikey;

#endregion

// NEW: References to UI objects
    public Button sendTextMessageButton;
    public GameObject mapToShow;
    public GameObject infoTextToShow;
    public InputField messageToSend;
    public Text loadingText;
    public Text watsonAnswer;
    public Button getHumanButton;
    //This bool determines whether a human has been requested from the other UI panel in UIElemetsLogic script
    public bool this_humanRequested;
    

    private int _recordingRoutine = 0;
    private string _microphoneID = null;
    private string[] microphones;
    private AudioClip _recording = null;
    private int _recordingBufferSize = 1;
    private int _recordingHZ = 22050;
    //private string initialMessage = "Hello";
    private AssistantService _assistant;
    private SpeechToTextService _speechToText;
    private TextToSpeechService _textToSpeech;
    private string sessionId;
    private bool firstMessage;
    private bool _stopListeningFlag = false;
    private bool sessionCreated = false;

    Animator animator;
// This function is run on startup
    void Awake()
    {
        Debug.Log("Waking up");
        Runnable.Run(InitializeServices());
        
    }

    void Start()
    {

        LogSystem.InstallDefaultReactors();
        animator = gameObject.GetComponent<Animator>();

#if ENABLE_DEBUGGING
        Debug.Log("in Start");
#endif

        microphones = Microphone.devices;
#if ENABLE_DEBUGGING        
        Debug.Log("microphone 1: " + microphones[0]);
#endif
        _microphoneID = microphones[0];

        //NEW: Set up UI elements.
        sendTextMessageButton.onClick.AddListener(delegate {SendTextMessage(messageToSend.text); });
        mapToShow.SetActive(false);
        infoTextToShow.SetActive(false);
    }

    void update() {
        //KNOWN BUG
        //NEW: Check whether a human has been requested from the other canvas. Not currently working as of 19th March
        if (GameObject.Find("MainUICanvas").GetComponent<UIElementsLogic>().humanRequested)
        {
            this_humanRequested = true;
            requestHuman();
        };
    }

    
//NEW: This authentication function has been adapted from the examples here https://github.com/watson-developer-cloud/unity-sdk/tree/master/Examples
    private IEnumerator InitializeServices()
    {

        if (string.IsNullOrEmpty(assistantIamApikey))
            {
                throw new IBMException("Plesae provide IAM ApiKey for the Assistant service.");
            }
        
        IamAuthenticator assistantAuthenticator = new IamAuthenticator(apikey: assistantIamApikey);

        while (!assistantAuthenticator.CanAuthenticate()) {
#if ENABLE_DEBUGGING
            Debug.Log("Not yet authenticated Assitant");
#endif
                yield return null; }

        _assistant = new AssistantService("2019-02-08", assistantAuthenticator);
        if (!string.IsNullOrEmpty(AssistantURL))
            {
                _assistant.SetServiceUrl(AssistantURL);
            }
        
        _assistant.CreateSession(OnCreateSession, assistantId);

 #if ENABLE_DEBUGGING 
        Debug.Log("Created Assistant Instance");
 #endif 
        while (!sessionCreated) {
            yield return null;

 #if ENABLE_DEBUGGING
            Debug.Log("No Assistant Instance Created");
#endif

            
            loadingText.text = "Loading Watson Assistant...";
        }

        if (string.IsNullOrEmpty(TextToSpeechIamApikey))
            {
                throw new IBMException("Plesae provide IAM ApiKey for the TTS service.");
            }
        
        IamAuthenticator ttsAuthenticator = new IamAuthenticator(apikey: TextToSpeechIamApikey);

        while (!ttsAuthenticator.CanAuthenticate()) {
                yield return null;
                Debug.Log("Not yet authenticated Text to Speech");
                loadingText.text = "Loading Watson...";
        }
        _textToSpeech = new TextToSpeechService(ttsAuthenticator);
        Debug.Log("Created textToSpeechService");

        if (!string.IsNullOrEmpty(TextToSpeechURL))
            {
                _textToSpeech.SetServiceUrl(TextToSpeechURL);
            }

        if (string.IsNullOrEmpty(SpeechToTextIamApikey))
            {
                throw new IBMException("Plesae provide IAM ApiKey for the STT service.");
            }
        
        IamAuthenticator sttAuthenticator = new IamAuthenticator(apikey: SpeechToTextIamApikey);

        while (!sttAuthenticator.CanAuthenticate()) {
                yield return null;
                Debug.Log("Not yet authenticated Speech to Text");
                loadingText.text = "Loading Watson...";
        }

        _speechToText = new SpeechToTextService(sttAuthenticator);
        Debug.Log("Created SpeechToTextService");

        if (!string.IsNullOrEmpty(SpeechToTextURL))
            {
                _speechToText.SetServiceUrl(SpeechToTextURL);
            }

        Debug.Log("Setting Active to True...");
        Active = true;

        //NEW: Watson message on Startup        
        //welcomeMessage();

        //SendTextMessage("Assistant started");

        StartRecording();   // Setup recording

    }


    private void welcomeMessage() {
        firstMessage = true;
        var input = new MessageInput(){Text = "Assistant Start Up"};

        _assistant.Message(OnMessage, assistantId, sessionId, input);
    }


    private void OnMessage(DetailedResponse<MessageResponse> response, IBMError error)
    {
        if (!firstMessage)
        {
            //getIntent
            List<RuntimeIntent> intentList = response.Result.Output.Intents;
            Log.Debug("WatsonLogic, size of intentList is:", response.Result.Output.Intents.Count.ToString());

            


            if (response.Result.Output.Intents.Count > 0) {
                string intent = response.Result.Output.Intents[0].Intent;
                Debug.Log("The Intent is: " + intent);
                //Trigger the animation
                MakeAMove(intent);
            }


            //get Watson Output
            string outputText2 = response.Result.Output.Generic[0].Text;

            //added below if to protect from calling Synthesize with null string
            if (outputText2 != null) 
            {
                Debug.Log("Watson returned not null message of: "+ outputText2);
            CallTextToSpeech(outputText2);
            }
        }

        firstMessage = false;

    }

    
//NEW: How the avatar animates or shows information based on the intent returned from Watson Cloud.
    private void MakeAMove(string intent)
    {
        switch(intent.ToLower())
        {
            // as of March 19th, currently we do not have a wave hello animation added to the avatar.
            case "general_greetings":
                avatarWaveHello();
                break; 
            
            case "find_staff_member":
                displayMap();
                break;

            case "toilet":
                displayMap();
                break;

            case "aboutibm":
                displayDetailedInfo();
                break;

            case "ibmhistory":
                displayDetailedInfo();
                break;
            
            case "ibm_staffnumbers":
                displayDetailedInfo();
                break;
            
            default:
                animator.SetBool("isIdle", true);
                animator.SetBool("isWavingHello", false);
                mapToShow.SetActive(false);
                infoTextToShow.SetActive(false);
                break;

        }

    }

    //NEW: avatar animation functions. The below should "true" for isWavingHello, but we don't currently have an appropriate animation
    private void avatarWaveHello() {
        animator.SetBool("isIdle", true);
        animator.SetBool("isWavingHello", false);
        mapToShow.SetActive(false);
        infoTextToShow.SetActive(false);
    }

    //NEW: avatar animation functions
    private void displayMap() {
        animator.SetBool("isIdle", true);
        animator.SetBool("isWavingHello", false);
        mapToShow.SetActive(true);
        infoTextToShow.SetActive(false);
    }


     //NEW: avatar animation functions
    private void displayDetailedInfo() {
        animator.SetBool("isIdle", true);
        animator.SetBool("isWavingHello", false);
        mapToShow.SetActive(false);
        infoTextToShow.SetActive(true);
    }

    private void BuildSpokenRequest(string spokenText)
    {
        var input = new MessageInput()
        {
            Text = spokenText
        };

        _assistant.Message(OnMessage, assistantId, sessionId, input);
    }

    private void SendTextMessage(string message) {
        var input = new MessageInput()
        {
            Text = message
        };

        _assistant.Message(OnMessage, assistantId, sessionId, input);
    }
    
    
    //NEW: send a message to watson to request a human and tell other gameObjects that it has been requested
    private void requestHuman() {

        SendTextMessage("Connect me to a human.");
        this_humanRequested = false;

    }

    

    private void CallTextToSpeech(string outputText)
    {
        Debug.Log("Sent to Watson Text To Speech: " + outputText);

        watsonAnswer.text = outputText;

        byte[] synthesizeResponse = null;
        AudioClip clip = null;

        //NEW: added below if to prevent null text error from occuring in TextToSpeechService
        if (outputText != null) {

        _textToSpeech.Synthesize(
            callback: (DetailedResponse<byte[]> response, IBMError error) =>
            {
                synthesizeResponse = response.Result;
                clip = WaveFile.ParseWAV("myClip", synthesizeResponse);
                PlayClip(clip);

            },
            text: outputText,
            voice: "en-US_MichaelVoice",
            accept: "audio/wav"
        );

        }
    }

    private void PlayClip(AudioClip clip)
    {
        Debug.Log("Received audio file from Watson Text To Speech");

        if (Application.isPlaying && clip != null)
        {
            GameObject audioObject = new GameObject("AudioObject");
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.spatialBlend = 0.0f;
            source.volume = 1.0f;
            source.loop = false;
            source.clip = clip;
            source.Play();

            Invoke("RecordAgain", source.clip.length);
            Destroy(audioObject, clip.length);
        }
    }

    private void RecordAgain()
    {
        Debug.Log("Played Audio received from Watson Text To Speech");
        if (!_stopListeningFlag)
        {
            OnListen();
        }
    }

    private void OnListen()
    {
        Log.Debug("AvatarPattern.OnListen", "Start();");

        Active = true;

        StartRecording();
    }

    public bool Active
    {
        get { return _speechToText.IsListening; }
        set
        {
            if (value && !_speechToText.IsListening)
            {
                _speechToText.DetectSilence = true;
                _speechToText.EnableWordConfidence = false;
                _speechToText.EnableTimestamps = false;
                _speechToText.SilenceThreshold = 0.03f;
                _speechToText.MaxAlternatives = 1;
                _speechToText.EnableInterimResults = true;
                _speechToText.OnError = OnError;
                _speechToText.StartListening(OnRecognize);
            }
            else if (!value && _speechToText.IsListening)
            {
                _speechToText.StopListening();
            }
        }
    }

    private void OnRecognize(SpeechRecognitionEvent result)
    {
        Debug.Log("In OnRecognize Method with result: " + result);
        if (result != null && result.results.Length > 0)
        {
            foreach (var res in result.results)
            {
                foreach (var alt in res.alternatives)
                {
                    if (res.final && alt.confidence > 0)
                    {
                        StopRecording();
                        string text = alt.transcript;
                        Debug.Log("Watson hears : " + text + " Confidence: " + alt.confidence);
                        loadingText.text = "Watson hears : " + text + " Confidence: " + alt.confidence;
                        BuildSpokenRequest(text);
                    }
                }
            }
        }

    }


    private void StartRecording()
    {
        if (_recordingRoutine == 0)
        {
            Debug.Log("Started Recording");
            UnityObjectUtil.StartDestroyQueue();
            _recordingRoutine = Runnable.Run(RecordingHandler());
        }
    }

    private void StopRecording()
    {
        if (_recordingRoutine != 0)
        {
            Debug.Log("Stopped Recording");
            Microphone.End(_microphoneID);
            Runnable.Stop(_recordingRoutine);
            _recordingRoutine = 0;
        }
    }

    private void OnError(string error)
    {
        Active = false;

        Log.Debug("AvatarPatternError.OnError", "Error! {0}", error);
    }

    private void OnCreateSession(DetailedResponse<SessionResponse> response, IBMError error)
    {
        Log.Debug("AvatarPatternError.OnCreateSession()", "Session: {0}", response.Result.SessionId);
        sessionId = response.Result.SessionId;
        sessionCreated = true;
    }

    private IEnumerator RecordingHandler()
    {
        Debug.Log("recording handler started");
        _recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
        yield return null;      // let m_RecordingRoutine get set..

        if (_recording == null)
        {
            StopRecording();
            Debug.Log("stopped recording because not able to initialise recording");

            yield break;

        }

        bool bFirstBlock = true;
        int midPoint = _recording.samples / 2;
        float[] samples = null;

        while (_recordingRoutine != 0 && _recording != null)
        {
            Debug.Log("Recording...");
            loadingText.text = "Waiting for you to say something...";

            int writePos = Microphone.GetPosition(_microphoneID);
            if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
            {
                Log.Error("MicrophoneWidget", "Microphone disconnected.");

                StopRecording();
                yield break;
            }

            if ((bFirstBlock && writePos >= midPoint)
                || (!bFirstBlock && writePos < midPoint))
            {

                Debug.Log("Making a recording...");
                // front block is recorded, make a RecordClip and pass it onto our callback.
                samples = new float[midPoint];
                _recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                AudioData record = new AudioData();
                record.MaxLevel = Mathf.Max(samples);
                record.Clip = AudioClip.Create("Recording", midPoint, _recording.channels, _recordingHZ, false);
                record.Clip.SetData(samples, 0);

                //here we send the recorded audio to be processed...
                _speechToText.OnListen(record);

                bFirstBlock = !bFirstBlock;
            }
            else
            {
                // calculate the number of samples remaining until we ready for a block of audio,
                // and wait that amount of time it will take to record.
                Debug.Log("Waiting for enough audio data...");
                int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
                float timeRemaining = (float)remaining / (float)_recordingHZ;

                yield return new WaitForSeconds(timeRemaining);
            }

        }

        yield break;
    }
}