using Android.App;
using Android.Widget;
using Android.OS;
using Android.Speech;
using Android.Content;
using System.Collections.Generic;
using Android.Provider;
using System;
using Xamarin.Contacts;
using System.Threading.Tasks;
using System.Linq;

namespace AsistentVocal
{
    [Activity(Label = "Asistent Vocal", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private readonly string youtubeURL = "https://www.youtube.com/results?search_query=";
        private readonly string mapsURL = "https://www.google.ro/maps/search/";
        private readonly string facebookURL = "fb://facewebmodal/f?href=";
        private readonly string googleURL = "http://www.google.com/#q=";
        private readonly string whatsAppPackage = "com.whatsapp";
        private readonly string error = "No speech was recognised";
        private readonly int VOICE = 1;
        private readonly Dictionary<string, int> numbers = new Dictionary<string, int>();
        private TextView textBox;
        private bool debug = false;

        private void SetTextBox(string text)
        {
            if (debug)
                textBox.Text = text;
        }

        protected override void OnActivityResult(int requestCode, Result resultVal, Intent data)
        {
            if (requestCode == VOICE)
            {
                bool commandDone = false;

                if (resultVal == Result.Ok)
                {
                    var matches = data.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
                    if (matches.Count == 0)
                        return;
                    matches[0] = matches[0].ToLower();
                    SetTextBox(matches[0]);

                    string[] words = matches[0].Split();

                    try
                    {
                        for (int i = 0; i < words.Length; ++i)
                            if ((words[i] == "start" || words[i] == "open") && i + 1 <= words.Length - 1)
                            {
                                switch (words[i + 1])
                                {
                                    case "clock":
                                        {
                                            var intent = new Intent(AlarmClock.ActionSetAlarm);
                                            StartActivity(intent);
                                            commandDone = true;
                                            break;
                                        }

                                    case "youtube":
                                        {
                                            var intent = new Intent(Intent.ActionView);
                                            intent.SetData(Android.Net.Uri.Parse(youtubeURL));
                                            StartActivity(intent);
                                            commandDone = true;
                                            break;
                                        }

                                    case "maps":
                                        {
                                            var intent = new Intent(Intent.ActionView);
                                            intent.SetData(Android.Net.Uri.Parse(mapsURL));
                                            StartActivity(intent);
                                            commandDone = true;
                                            break;
                                        }

                                    case "whatsapp":
                                        {
                                            var intent = new Intent(Intent.ActionView);
                                            intent.SetAction(Intent.ActionSend);
                                            intent.SetPackage(whatsAppPackage);
                                            intent.SetType("text/plain");
                                            StartActivity(intent);
                                            commandDone = true;
                                            break;
                                        }

                                    case "facebook":
                                        {
                                            var intent = new Intent(Intent.ActionView);
                                            intent.SetData(Android.Net.Uri.Parse(facebookURL));
                                            StartActivity(intent);
                                            commandDone = true;
                                            break;
                                        }

                                    default: break;
                                }
                                
                                break;
                            }
                            else if (words[i] == "take" && i + 1 <= words.Length)
                            {
                                Intent intent = null;
                                if (words[i + 1] == "photo")
                                {
                                    intent = new Intent(MediaStore.ActionImageCapture);
                                    commandDone = true;
                                }
                                else if (words[i + 1] == "video")
                                {
                                    intent = new Intent(MediaStore.ActionVideoCapture);
                                    commandDone = true;
                                }
                                if (intent != null)
                                    StartActivity(intent);
                                break;
                            }
                            else if (words[i] == "where" && i + 1 <= words.Length - 1 && words[i + 1] == "is")
                            {
                                string query = "";
                                for (int j = i + 2; j < words.Length; ++j)
                                {
                                    query += words[j];
                                    if (j != words.Length - 1)
                                        query += "+";
                                }
                                var intent = new Intent(Intent.ActionView);
                                intent.SetData(Android.Net.Uri.Parse(mapsURL + query));
                                StartActivity(intent);

                                commandDone = true;
                                break;
                            }
                            else if (words[i] == "what" && i + 3 <= words.Length - 1 && words[i + 1] == "is" && words[i + 2] == "the")
                            {
                                DateTime date = DateTime.Now;
                                var text = "The ";
                                if (words[i + 3] == "date")
                                {
                                    commandDone = true;
                                    text += "date is " + date.Day + "/" + date.Month + "/" + date.Year;
                                    Toast.MakeText(this, text, ToastLength.Long).Show();
                                }
                                else if (words[i + 3] == "time")
                                {
                                    commandDone = true;
                                    text += "time is " + date.Hour + ":" + date.Minute + ":" + date.Second;
                                    Toast.MakeText(this, text, ToastLength.Long).Show();
                                }

                                break;
                            }
                            else if (words[i] == "how" && i + 3 <= words.Length - 1 && words[i + 1] == "is" && words[i + 2] == "the" && words[i + 3] == "battery")
                            {
                                var filter = new IntentFilter(Intent.ActionBatteryChanged);
                                using (var battery = Application.Context.RegisterReceiver(null, filter))
                                {
                                    var level = battery.GetIntExtra(BatteryManager.ExtraLevel, -1);
                                    var scale = battery.GetIntExtra(BatteryManager.ExtraScale, -1);

                                    string percentage = ((int)Math.Floor(level * 100D / scale)).ToString();

                                    Toast.MakeText(this, "The battery is " + percentage + "%", ToastLength.Long).Show();
                                }

                                commandDone = true;
                                break;
                            }
                            else if (words[i] == "weather" && i + 3 <= words.Length - 1 && words[i + 1] == "in")
                            {
                                string query = words[i] + "+" + words[i + 2] + "+" + words[i + 3];
                                var intent = new Intent(Intent.ActionView);
                                intent.SetData(Android.Net.Uri.Parse(googleURL + query));
                                StartActivity(intent);

                                commandDone = true;
                                break;
                            }
                            else if (words[i] == "send" && i + 3 <= words.Length && words[i + 1] == "sms" && words[i + 2] == "to")
                            {
                                string number = null;
                                var message = "";
                                var book = new AddressBook(this);
                                commandDone = true;

                                book.RequestPermission().ContinueWith(t =>
                                {
                                    if (!t.Result)
                                        return;

                                    foreach (Contact contact in book)
                                        if (contact.FirstName.ToLower() == words[i + 3])
                                        {
                                            number = contact.Phones.ToArray()[1].Number;
                                            break;
                                        }

                                    for (int j = i + 4; j < words.Length; ++j)
                                        message += words[j] + " ";

                                    var uri = Android.Net.Uri.Parse("smsto:" + number);
                                    Intent intent = new Intent(Intent.ActionSendto, uri);
                                    intent.PutExtra("sms_body", message);
                                    StartActivity(intent);

                                }, TaskScheduler.FromCurrentSynchronizationContext());

                                break;
                            }
                            else if (words[i] == "search")
                            {
                                var query = "";
                                for (int j = i + 1; j < words.Length; ++j)
                                {
                                    query += words[j];
                                    if (j != words.Length - 1)
                                        query += "+";
                                }
                                var intent = new Intent(Intent.ActionView);
                                intent.SetData(Android.Net.Uri.Parse(googleURL + query));
                                StartActivity(intent);

                                commandDone = true;
                                break;
                            }
                            else if (words[i] == "call" && i + 1 <= words.Length)
                            {
                                string number = null;
                                var book = new AddressBook(this);
                                commandDone = true;

                                book.RequestPermission().ContinueWith(t =>
                                {
                                    if (!t.Result)
                                        return;

                                    foreach (Contact contact in book)
                                        if (contact.FirstName.ToLower() == words[i + 1])
                                        {
                                            number = contact.Phones.ToArray()[0].Number;
                                            break;
                                        }

                                    var uri = Android.Net.Uri.Parse("tel:" + number);
                                    Intent intent = new Intent(Intent.ActionCall, uri);
                                    StartActivity(intent);

                                }, TaskScheduler.FromCurrentSynchronizationContext());

                                break;
                            }
                            else if (words[i] == "play")
                            {
                                string query = "";
                                for (int j = i + 1; j < words.Length; ++j)
                                {
                                    query += words[j];
                                    if (j != words.Length - 1)
                                        query += "+";
                                }
                                var intent = new Intent(Intent.ActionView);
                                intent.SetData(Android.Net.Uri.Parse(youtubeURL + query));
                                StartActivity(intent);

                                commandDone = true;
                                break;
                            }
                            else if (words[i] == "alarm")
                            {
                                int hour = 0, minute = 0;
                                bool ok = true;
                                if (i + 1 <= words.Length - 1 && (words[i + 1] == "at" || words[i + 1] == "for"))
                                {
                                    if (i + 2 <= words.Length - 1)
                                        ok = ok && numbers.TryGetValue(words[i + 2], out hour);

                                    int offset = 3;

                                    if (i + 3 <= words.Length - 1 && words[i + 3] == "and")
                                        offset++;

                                    int tmp = 0;

                                    if (i + offset <= words.Length)
                                    {
                                        ok = ok && numbers.TryGetValue(words[i + offset], out tmp);
                                        minute = tmp;
                                    }

                                    if (i + offset + 1 <= words.Length - 1)
                                    {
                                        ok = ok && numbers.TryGetValue(words[i + offset + 1], out tmp);
                                        minute = minute * 10 + tmp;
                                    }
                                }


                                var intent = new Intent(AlarmClock.ActionSetAlarm);
                                intent.PutExtra(AlarmClock.ExtraMessage, "New Alarm");

                                if (ok)
                                {
                                    intent.PutExtra(AlarmClock.ExtraHour, hour);
                                    intent.PutExtra(AlarmClock.ExtraMinutes, minute);
                                }
                                StartActivity(intent);

                                commandDone = true;
                                break;
                            }
                            else if (words[i] == "switch" && i + 3 <= words.Length && words[i + 2] == "debug" && words[i + 3] == "mode")
                            {
                                if (words[i + 1] == "on")
                                {
                                    debug = true;
                                    commandDone = true;
                                    Toast.MakeText(this, "DEBUG ON", ToastLength.Short).Show();
                                    var message = "";
                                    for (int j = 0; j < words.Length; ++j)
                                        message += " " + words[j];
                                    SetTextBox(message);
                                }
                                else if (words[i + 1] == "off")
                                {
                                    SetTextBox("");
                                    debug = false;
                                    commandDone = true;
                                    Toast.MakeText(this, "DEBUG OFF", ToastLength.Short).Show();
                                }
                                break;
                            }
                    }
                    catch
                    {
                        commandDone = false;
                    }
                }

                if (resultVal != Result.Ok || commandDone == false)
                    Toast.MakeText(this, error, ToastLength.Long).Show();
                

                base.OnActivityResult(requestCode, resultVal, data);
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            numbers.Add("one", 1);
            numbers.Add("two", 2);
            numbers.Add("three", 3);
            numbers.Add("four", 4);
            numbers.Add("five", 5);
            numbers.Add("six", 6);
            numbers.Add("seven", 7);
            numbers.Add("eight", 8);
            numbers.Add("nine", 9);
            numbers.Add("ten", 10);
            numbers.Add("eleven", 11);
            numbers.Add("twelve", 12);
            numbers.Add("thirteen", 13);
            numbers.Add("fourteen", 14);
            numbers.Add("fifteen", 15);
            numbers.Add("sixteen", 16);
            numbers.Add("seventeen", 17);
            numbers.Add("eighteen", 18);
            numbers.Add("nineteen", 19);
            numbers.Add("twenty", 20);
            numbers.Add("thirty", 30);
            numbers.Add("forty", 40);
            numbers.Add("fifty", 50);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            textBox = FindViewById<TextView>(Resource.Id.text_id);

            FindViewById<Button>(Resource.Id.button_id).Click += delegate
            {
                var voiceIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                voiceIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                //voiceIntent.PutExtra(RecognizerIntent.ExtraPrompt, Application.Context.GetString(Resource.String.messageSpeakNow));
                voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1500);
                voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1500);
                voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 15000);
                voiceIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                //voiceIntent.PutExtra(RecognizerIntent.ExtraSupportedLanguages, "EN-us");
                StartActivityForResult(voiceIntent, VOICE);
            };
        }
    }
}