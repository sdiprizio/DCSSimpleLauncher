#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace DCSSimpleLauncher.Data
{
    internal class CompanionApp : INotifyPropertyChanged
    {
        public required string Name { get; set; }
        public required string Path { get; set; }
        public string? Args { get; set; }
        public double Delay { get; set; } = 0.0;
        public bool Minimize { get; set; }
        public double HideWindowDelaySeconds { get; set; } = 2.0;
        public string? WorkingDirectory { get; set; }
        public bool RunAsAdmin { get; set; }

        private bool _isRunning;

        [JsonIgnore]
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning == value) return;
                _isRunning = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
