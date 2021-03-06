﻿using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Notifications.Slack.Payloads;
using NzbDrone.Core.Rest;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Validation;
using RestSharp;


namespace NzbDrone.Core.Notifications.Slack
{
    public class Slack : NotificationBase<SlackSettings>
    {
        private readonly ISlackProxy _proxy;
        private readonly Logger _logger;
  
        public Slack(ISlackProxy proxy, Logger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public override string Name => "Slack";

        public override string Link => "https://my.slack.com/services/new/incoming-webhook/";

        public override void OnGrab(GrabMessage message)
        {
            var attachments = new List<Attachment>
                            {
                                new Attachment
                                {
                                    Fallback = message.Message,
                                    Title = message.Movie.Title,
                                    Text = message.Message,
                                    Color = "warning"
                                }
                            };
            var payload = CreatePayload($"Grabbed: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnDownload(DownloadMessage message)
        {
            var attachments = new List<Attachment>
                                {
                                    new Attachment
                                    {
                                        Fallback = message.Message,
                                        Title = message.Movie.Title,
                                        Text = message.Message,
                                        Color = "good"
                                    }
                                };
            var payload = CreatePayload($"Imported: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnMovieRename(Movie movie)
        {
            var attachments = new List<Attachment>
                                {
                                    new Attachment
                                    {
                                        Title = movie.Title,
                                    }
                                };
 
             var payload = CreatePayload("Renamed", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(TestMessage());

            return new ValidationResult(failures);
        }

        public ValidationFailure TestMessage()
        {
            try
            {
                var message = $"Test message from Radarr posted at {DateTime.Now}";

                var payload = CreatePayload(message);

                _proxy.SendPayload(payload, Settings);

            }
            catch (SlackExeption ex)
            {
                return new NzbDroneValidationFailure("Unable to post", ex.Message);
            }

            return null;
        }

        private SlackPayload CreatePayload(string message, List<Attachment> attachments = null)
        {
            var icon = Settings.Icon;

            var payload = new SlackPayload
            {
                Username = Settings.Username,
                Text = message,
                Attachments = attachments
            };

            if (icon.IsNotNullOrWhiteSpace())
            {
                // Set the correct icon based on the value
                if (icon.StartsWith(":") && icon.EndsWith(":"))
                {
                    payload.IconEmoji = icon;
                }
                else
                {
                    payload.IconUrl = icon;
                }
            }

            return payload;
        }
    }
}
