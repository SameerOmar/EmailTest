using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using DeveloperTest.DataTypes;
using DeveloperTest.Mesic;
using DeveloperTest.Models;
using DeveloperTest.Services;
using DeveloperTest.Workers;
using Prism.Commands;
using Prism.Mvvm;

namespace DeveloperTest.ViewModels
{
    internal class MainWindowViewModel : BindableBase
    {
        private readonly MainWorker _mainWorker;
        private readonly SynchronizationContext _syncContext;
        private ObservableCollection<EmailMessageModel> _emailMessagesCollection;
        private string _errorMessage;

        private bool _isWorkerBusy;
        private EmailMessageModel _selectedEmail;
        private EncryptionProtocol _selectedEncryptionProtocol;
        private ServerType _selectedServerType;
        private bool _isStartButtonEnabled;
        private string _startButtonText;

        public MainWindowViewModel()
        {
            _syncContext = SynchronizationContext.Current;

            StartButtonText = "Start";
            IsStartButtonEnabled = true;

            FillStaticData();

            EmailMessagesCollection = new ObservableCollection<EmailMessageModel>();
            StartCommand = new DelegateCommand<PasswordBox>(ExecuteStartCommand);

            _mainWorker = new MainWorker();
            _mainWorker.EmailInfoAdded += EmailInfoAddedEventHandler;
            _mainWorker.EmailInfoBodyUpdated += EmailInfoBodyUpdatedEventHandler;
            _mainWorker.WorkerStarted += WorkerStartedEventHandler;
            _mainWorker.WorkerStopped += WorkerStoppedEventHandler;

            Server = "imap.gmail.com";
            Port = 993;
        }

        public ObservableCollection<EmailMessageModel> EmailMessagesCollection
        {
            get => _emailMessagesCollection;
            private set => SetProperty(ref _emailMessagesCollection, value);
        }

        public List<string> EncryptionProtocols { get; set; }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public SecureString Password { get; set; }

        public int Port { get; set; }

        public EmailMessageModel SelectedEmail
        {
            get => _selectedEmail;
            set
            {
                SetProperty(ref _selectedEmail, value);
                SelectedEmailChanged(value);
            }
        }

        public EncryptionProtocol SelectedEncryptionProtocol
        {
            get => _selectedEncryptionProtocol;
            set => SetProperty(ref _selectedEncryptionProtocol, value);
        }

        public ServerType SelectedServerType
        {
            get => _selectedServerType;
            set => SetProperty(ref _selectedServerType, value);
        }

        public string Server { get; set; }

        public List<string> ServerTypes { get; set; }

        public bool IsStartButtonEnabled
        {
            get => _isStartButtonEnabled;
            set => SetProperty(ref _isStartButtonEnabled, value);
        }

        public string StartButtonText
        {
            get => _startButtonText;
            set => SetProperty(ref _startButtonText, value);
        }

        public DelegateCommand<PasswordBox> StartCommand { get; set; }

        public string UserName { get; set; }

        private void SelectedEmailChanged(EmailMessageModel emailMessageModel)
        {
            if (emailMessageModel.BodyDownloaded)
            {
                return;
            }

            Task.Run(() =>
            {
                var connector = CreateServerConnector();

                var emailBody = connector?.GetMailBody(emailMessageModel.MessageId);

                if (emailBody == null)
                {
                    return;
                }

                emailMessageModel.HtmlBody = emailBody.Html;
                emailMessageModel.TextBody = emailBody.Text;
                emailMessageModel.BodyDownloaded = true;
            });
        }

        private IEmailServerConnector CreateServerConnector()
        {
            var emailServerConnector = _selectedServerType == ServerType.Imap
                ? new ImapServerConnector()
                : (IEmailServerConnector) new Pop3ServerConnector();

            if (!emailServerConnector.Connect(Server, Port, _selectedEncryptionProtocol)
                || !emailServerConnector.Login(UserName, Password))
            {
                ErrorMessage = emailServerConnector.LastErrorMessage;
                IsStartButtonEnabled = true;
                return null;
            }

            return emailServerConnector;
        }

        private void EmailInfoBodyUpdatedEventHandler(object sender, EmailInfo e)
        {
            if (e.MessageEnvelope == null || e.EmailBody == null)
            {
                return;
            }

            _syncContext.Post(o =>
            {
                var emailMessage = _emailMessagesCollection.FirstOrDefault(m => m.MessageId == e.MessageUid);

                if (emailMessage == null || emailMessage.BodyDownloaded)
                {
                    return;
                }

                emailMessage.HtmlBody = e.EmailBody.Html;
                emailMessage.TextBody = e.EmailBody.Text;
                emailMessage.BodyDownloaded = true;
            }, null);
        }

        private void WorkerStoppedEventHandler(object sender, EventArgs e)
        {
            _syncContext.Post(o =>
            {
                _isWorkerBusy = false;
                StartButtonText = "Start";
                IsStartButtonEnabled = true;
            }, null);
        }

        private void WorkerStartedEventHandler(object sender, EventArgs e)
        {
            _syncContext.Post(o =>
            {
                _isWorkerBusy = true;
                StartButtonText = "Stop";
                IsStartButtonEnabled = true;
            }, null);
        }

        private void EmailInfoAddedEventHandler(object sender, EmailInfo e)
        {
            if (e.MessageEnvelope == null)
            {
                return;
            }

            var emailMessageModel = new EmailMessageModel
            {
                MessageId = e.MessageUid,
                FromName = !string.IsNullOrWhiteSpace(e.MessageEnvelope.FromName)
                    ? $"{e.MessageEnvelope.FromName}<{e.MessageEnvelope.FromAddress}>"
                    : e.MessageEnvelope.FromAddress,
                FromAddress = e.MessageEnvelope.FromAddress,
                Subject = e.MessageEnvelope.Subject,
                Date = e.MessageEnvelope.Date
            };

            _syncContext.Post(o => { _emailMessagesCollection.Insert(0, emailMessageModel); }, null);
        }

        private void ExecuteStartCommand(PasswordBox passwordBox)
        {
            ErrorMessage = "";

            if (_mainWorker == null || passwordBox == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Server) || Port <= 0 || passwordBox.SecurePassword.Length == 0)
            {
                ErrorMessage = "Please enter required information";
                return;
            }

            IsStartButtonEnabled = false;

            if (_isWorkerBusy)
            {
                Task.Run(() => { _mainWorker.Stop(); });
                return;
            }

            EmailMessagesCollection.Clear();
            Password = passwordBox.SecurePassword;

            Task.Run(() =>
            {
                var connector = CreateServerConnector();

                if (connector == null)
                {
                    return;
                }

                _mainWorker.Start(connector);
            });
        }


        private void FillStaticData()
        {
            ServerTypes = new List<string>();
            EncryptionProtocols = new List<string>();

            ServerTypes.AddRange(Enum.GetValues(typeof(ServerType)).Cast<ServerType>()
                .Select(e => e.GetDescription()));

            EncryptionProtocols.AddRange(Enum.GetValues(typeof(EncryptionProtocol)).Cast<EncryptionProtocol>()
                .Select(e => e.GetDescription()));
        }
    }
}