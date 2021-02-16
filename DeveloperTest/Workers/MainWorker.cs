using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DeveloperTest.DataTypes;
using DeveloperTest.Services;

namespace DeveloperTest.Workers
{
    internal class MainWorker : IDisposable
    {
        private const int MaxConnectionsCount = 5;
        private readonly List<IEmailServerConnector> _connectorsList;
        private readonly Queue<EmailInfo> _emailsQueue;
        private readonly object _lockObject;
        private readonly Queue<MessageUid> _messageUidsQueue;

        private readonly List<Thread> _threadsList;
        private bool _isWorking;

        public MainWorker()
        {
            _lockObject = new object();
            _threadsList = new List<Thread>();
            _messageUidsQueue = new Queue<MessageUid>();
            _emailsQueue = new Queue<EmailInfo>();
            _connectorsList = new List<IEmailServerConnector>(MaxConnectionsCount);
        }

        public void Dispose()
        {
            if (_isWorking)
            {
                Stop();
            }
        }

        public event EventHandler<EmailInfo> EmailInfoAdded;
        public event EventHandler<EmailInfo> EmailInfoBodyUpdated;
        public event EventHandler WorkerStarted;
        public event EventHandler WorkerStopped;

        public void Start(IEmailServerConnector connector)
        {
            FillConnectorsPool(connector);

            if (_isWorking)
            {
                Stop();
            }

            _isWorking = true;


            new Thread(DoWork).Start();

            OnWorkerStarted();
        }

        private void FillConnectorsPool(IEmailServerConnector connector)
        {
            ClearConnectorsPool();

            _connectorsList.Add(connector);

            for (var x = 0; x < MaxConnectionsCount - 1; x++)
            {
                _connectorsList.Add(connector.Clone());
            }
        }

        private void ClearConnectorsPool()
        {
            if (_connectorsList.Count > 0)
            {
                foreach (var connector in _connectorsList)
                {
                    connector.Dispose();
                }
            }

            _connectorsList.Clear();
        }

        private void QueuesWatcher()
        {
            new Thread(() =>
            {
                while (_isWorking && (_messageUidsQueue.Count > 0 || _emailsQueue.Count > 0))
                {
                    Thread.Sleep(1000);
                }

                if (_isWorking)
                {
                    Stop();
                }
            }).Start();
        }

        public void Stop()
        {
            _isWorking = false;

            if (_threadsList == null)
            {
                return;
            }

            new Thread(() =>
            {
                _threadsList.ForEach(t => t.Join());

                _messageUidsQueue.Clear();
                _emailsQueue.Clear();
                _threadsList.Clear();

                ClearConnectorsPool();

                OnWorkerStopped();
            }).Start();
        }

        private MessageUid PickMessageUid()
        {
            lock (_lockObject)
            {
                if (_messageUidsQueue.Count > 0)
                {
                    return _messageUidsQueue.Dequeue();
                }
            }

            return null;
        }

        private EmailInfo PickEmailInfo()
        {
            lock (_lockObject)
            {
                if (_emailsQueue.Count > 0)
                {
                    return _emailsQueue.Dequeue();
                }
            }

            return null;
        }

        private void DoWork()
        {
            var connector = GetEmailServerConnector();

            GetMessageUids(connector);
            QueuesWatcher();

            StartDownloaderThread(EnvelopesDownloader, connector);

            var bodyDownloaderThreadsCount = 0;
            while (_isWorking && bodyDownloaderThreadsCount < MaxConnectionsCount)
            {
                connector = GetEmailServerConnector();

                if (connector != null)
                {
                    bodyDownloaderThreadsCount++;
                    StartDownloaderThread(BodyDownloader, connector);
                }

                Thread.Sleep(1000);
            }
        }

        private void StartDownloaderThread(DownloaderCallback downloaderCallback, IEmailServerConnector connector)
        {
            var thread = new Thread(() =>
            {
                downloaderCallback(connector);
                connector.InUse = false;
            });

            _threadsList.Add(thread);

            thread.Start();
        }

        private IEmailServerConnector GetEmailServerConnector()
        {
            lock (_lockObject)
            {
                var connector = _connectorsList.FirstOrDefault(c => c.InUse == false);
                if (connector != null)
                {
                    connector.InUse = true;
                    return connector;
                }
            }

            return null;
        }

        private void GetMessageUids(IEmailServerConnector connector)
        {
            var uids = connector.GetMessagesUids();
            foreach (var messageUid in uids)
            {
                _messageUidsQueue.Enqueue(messageUid);
            }
        }

        private void EnvelopesDownloader(IEmailServerConnector connector)
        {
            while (_isWorking)
            {
                var messageUid = PickMessageUid();

                if (messageUid == null)
                {
                    break;
                }

                var emailInfo = new EmailInfo();
                emailInfo.MessageUid = messageUid;
                emailInfo.MessageEnvelope = connector.GetMailEnvelope(messageUid);

                if (emailInfo.MessageEnvelope == null)
                {
                    // if can not get the envelope then re-enqueue the emailInfo
                    // so we can try to download the body in another iteration
                    _messageUidsQueue.Enqueue(messageUid);
                }
                else
                {
                    // envelope downloaded so raise the event
                    // and enqueue the email info for body downloading 
                    OnNewEmailInfoAdded(emailInfo);
                    _emailsQueue.Enqueue(emailInfo);
                }
            }
        }

        private void BodyDownloader(IEmailServerConnector connector)
        {
            while (_isWorking)
            {
                var emailInfo = PickEmailInfo();

                if (emailInfo == null)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                emailInfo.EmailBody = connector.GetMailBodyStructure(emailInfo.MessageUid);

                if (emailInfo.MessageEnvelope == null)
                {
                    // if can not get the body then re-enqueue the emailInfo
                    // so we can try to download the body in another iteration
                    _emailsQueue.Enqueue(emailInfo);
                }
                else
                {
                    // body downloaded so raise the event
                    OnEmailInfoBodyUpdated(emailInfo);
                }
            }
        }

        protected virtual void OnNewEmailInfoAdded(EmailInfo e)
        {
            EmailInfoAdded?.Invoke(this, e);
        }

        protected virtual void OnWorkerStarted()
        {
            WorkerStarted?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnWorkerStopped()
        {
            WorkerStopped?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnEmailInfoBodyUpdated(EmailInfo e)
        {
            EmailInfoBodyUpdated?.Invoke(this, e);
        }

        private delegate void DownloaderCallback(IEmailServerConnector emailServerConnector);
    }
}