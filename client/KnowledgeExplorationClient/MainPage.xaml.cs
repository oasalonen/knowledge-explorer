using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace KnowledgeExplorationClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public class Entity
        {
            public enum Type
            {
                None,
                Absence,
                FirstName,
                LastName,
                Date
            }

            public Type type;
            public string display;
        }

        public class InterpretationResponse
        {
            public string query;

            public List<Interpretation> interpretations;

            public class Interpretation
            {
                public string parse;
                public List<Rule> rules;

                public class Rule
                {
                    public string name;
                    public Output output;

                    public class Output
                    {
                        public string type;
                        public string value;
                    }
                }

                public string GetFirstQuery()
                {
                    var rule = rules.FirstOrDefault(r => r.output != null && r.output.type == "query");
                    return (rule != null ? rule.output.value : "");
                }

                public List<Entity> GetEntities()
                {
                    var entities = new List<Entity>();
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(parse)))
                    using (var reader = XmlReader.Create(ms))
                    {
                        reader.MoveToContent();
                        while (!reader.EOF)
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                if (reader.Name == "attr")
                                {
                                    XElement el = XNode.ReadFrom(reader) as XElement;
                                    var entity = new Entity();
                                    XAttribute attr;
                                    entity.display = el.Value.Trim();
                                    if ((attr = el.Attributes().FirstOrDefault(a => a.Name == "name")) != null)
                                    {
                                        switch (attr.Value)
                                        {
                                            case "absences#type":
                                                entity.type = Entity.Type.Absence;
                                                break;
                                            case "absences#person.first_name":
                                                entity.type = Entity.Type.FirstName;
                                                break;
                                            case "absences#person.last_name":
                                                entity.type = Entity.Type.LastName;
                                                break;
                                            case "absences#start":
                                                entity.type = Entity.Type.Date;
                                                break;
                                            default:
                                                entity.type = Entity.Type.None;
                                                break;
                                        }
                                    }
                                    if ((attr = el.Attributes().FirstOrDefault(a => a.Name == "canonical")) != null)
                                    {
                                        entity.display = attr.Value.Trim();
                                    }

                                    entities.Add(entity);
                                    continue;
                                }
                            }
                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                entities.Add(new Entity() { display = reader.Value });
                            }
                            reader.Read();
                        }

                    }
                    return entities;
                }
            }
        }

        public class EvaluationResponse
        {
            public List<Result> entities;

            public class Result
            {
                public string[] type;
                public string[] start;
                public string[] end;
                public Person[] person;
            }

            public class Person
            {
                public string[] first_name;
                public string[] last_name;
            }

            public List<Absence> GetAbsences()
            {
                var result = new List<Absence>();
                foreach (var e in entities)
                {
                    var absence = new Absence()
                    {
                        type = e.type.FirstOrDefault(),
                        start = e.start.FirstOrDefault(),
                        end = e.end.FirstOrDefault(),
                        person = e.person.FirstOrDefault() != null ? e.person.First().first_name.FirstOrDefault() + " " + e.person.First().last_name.FirstOrDefault() : ""
                    };
                    result.Add(absence);
                }
                return result;
            }
        }

        public class Absence
        {
            public string type;
            public string start;
            public string end;
            public string person;

            public override string ToString()
            {
                return "Person: " + person + ", Type: " + type + ", Start: " + start + ", End: " + end;
            }
        }

        private HttpClient _client = new HttpClient() { BaseAddress = new Uri("http://localhost:8000") };
        private InterpretationResponse.Interpretation _currentInterpretation;

        public MainPage()
        {
            this.InitializeComponent();

            _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            var focusStream = Observable.FromEventPattern<RoutedEventArgs>(input, "GotFocus");
            var keyInputStream = Observable.FromEventPattern<TextChangedEventArgs>(input, "TextChanged");
            var keyPressTream = Observable.FromEventPattern<KeyRoutedEventArgs>(input, "KeyDown");
            var enterStream = keyPressTream.Where(e => e.EventArgs.Key == Windows.System.VirtualKey.Enter);

            keyPressTream
                .Where(e => e.EventArgs.Key == Windows.System.VirtualKey.Tab)
                .Subscribe(e => 
                    {
                        e.EventArgs.Handled = true;
                        SetCurrentInterpretationAsText();
                    });

            keyInputStream.Throttle(TimeSpan.FromMilliseconds(200))
                .Merge<object>(focusStream)
                .ObserveOnDispatcher()
                .Select(_ => input.Text)
                .DistinctUntilChanged()
                .Select(text => GetIntepretationsAsync(text).ToObservable())
                .Switch()
                .ObserveOnDispatcher()
                .Subscribe(
                    response =>
                    {
                        suggestions.Children.Clear();
                        results.Text = "";

                        _currentInterpretation = response.interpretations.FirstOrDefault();

                        foreach (var i in response.interpretations)
                        {
                            StackPanel item = new StackPanel();
                            item.Orientation = Orientation.Horizontal;

                            foreach (var e in i.GetEntities())
                            {
                                var block = new TextBlock();
                                block.Margin = new Thickness(0, 0, 5, 0);
                                block.FontSize = 20;
                                block.Text = e.display;
                                switch (e.type)
                                {
                                    case Entity.Type.Absence:
                                        block.Foreground = new SolidColorBrush(Colors.Red);
                                        break;
                                    case Entity.Type.FirstName:
                                        block.Foreground = new SolidColorBrush(Colors.ForestGreen);
                                        break;
                                    case Entity.Type.LastName:
                                        block.Foreground = new SolidColorBrush(Colors.ForestGreen);
                                        break;
                                    case Entity.Type.Date:
                                        block.Foreground = new SolidColorBrush(Colors.MediumSlateBlue);
                                        break;
                                    default:
                                        break;
                                }
                                item.Children.Add(block);
                            }

                            item.Tapped += (_, __) =>
                            {
                                _currentInterpretation = i;
                                SetCurrentInterpretationAsText();
                            };
                            suggestions.Children.Add(item);
                        }
                    },
                    error => Debug.WriteLine(error)
                    );

            Observable.FromEventPattern<RoutedEventArgs>(submit, "Click")
                .Merge<object>(enterStream)
                .Select(_ => _currentInterpretation)
                .Where(i => i != null)
                .Select(i => GetEvaluationAsync(i).ToObservable())
                .Switch()
                .ObserveOnDispatcher()
                .Subscribe(e =>
                {
                    results.Text = "";
                    var absences = e.GetAbsences();
                    foreach (var a in absences)
                    {
                        results.Text += a.ToString() + "\n";
                    }
                });

        }

        private async Task<InterpretationResponse> GetIntepretationsAsync(string query)
        {
            string data = await _client.GetStringAsync("/interpret?query=" + Uri.EscapeDataString(query) + "&complete=1");
            return JsonConvert.DeserializeObject<InterpretationResponse>(data);
        }

        private async Task<EvaluationResponse> GetEvaluationAsync(InterpretationResponse.Interpretation i)
        {
            return JsonConvert.DeserializeObject<EvaluationResponse>(await _client.GetStringAsync("/evaluate?expr=" + Uri.EscapeDataString(i.GetFirstQuery()) + "&attributes=person.first_name,person.last_name,type,start,end&count=200"));
        }

        private void SetCurrentInterpretationAsText()
        {
            string text = "";
            if (_currentInterpretation != null)
            {
                foreach (var e in _currentInterpretation.GetEntities())
                {
                    text += e.display + " ";
                }
            }
            input.Text = Regex.Replace(text, @"\s+", " ").Trim();
            input.SelectionStart = input.Text.Length;
            input.SelectionLength = 0;
        }
    }
}
