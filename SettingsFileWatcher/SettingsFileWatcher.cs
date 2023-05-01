using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Crestron.SimplSharp;
using System.Timers;

namespace SFW {
	public class SettingsFileWatcher {
		private string _FilePath;
		private bool _Debug = false;
		private FileSystemWatcher _FileWatcher = null;
		private Timer _Timer = new Timer(1000);


		public delegate void FileChangedHandler(object sender, EventArgs e);
		public event FileChangedHandler FileChanged;

		private void _dbg(string msg) {
			if (this._Debug) {
				CrestronConsole.Print("\x00FA\x00E0{0}{1}\x00FB", (char)InitialParametersClass.ApplicationNumber, msg);
			}
		}
		private void _dbg(string msg, params object[] args) {
			if (this._Debug) {
				_dbg(string.Format(msg, args));
			}
		}

		public void Init(string FilePath) {
			_dbg($"Initializing SettingsFileWatcher for file {FilePath}");
			if (this._FileWatcher != null) {
				this._FileWatcher.Dispose();
			}
			this._FilePath = FilePath;
			_dbg($"SettingsFileWatcher path: {Path.GetDirectoryName(FilePath)}");
			_dbg($"SettingsFileWatcher filename: {Path.GetFileName(FilePath)}");
			try {
				this._FileWatcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath));
			} catch (Exception ex) {
				ErrorLog.Warn($"Error creating settings file watcher: {ex.Message}");
				this._FileWatcher = null;
				return;
			}
			this._FileWatcher.Filter = Path.GetFileName(FilePath);
			this._FileWatcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.LastWrite;
			this._FileWatcher.Changed += _FileChanged;
			this._FileWatcher.Created += _FileChanged;
			this._FileWatcher.IncludeSubdirectories = false;
			this._FileWatcher.EnableRaisingEvents = true;
			this._Timer.AutoReset = false;
			this._Timer.Elapsed += _Timer_Elapsed;

		}

		public void SetDebug(uint Debug) {
			this._Debug = (Debug == 0 ? false : true);
		}

		private void _FileChanged(object sender, FileSystemEventArgs e) {
			_dbg($"File {e.FullPath} has changed");
			this._Timer.Stop();
			this._Timer.Start();
		}

		private void _Timer_Elapsed(object sender, ElapsedEventArgs e) {
			FileChanged(this, new EventArgs());
		}

		private void _FileError(object sender, ErrorEventArgs e) {
			_dbg($"Error with SettingsFileWatcher: {e.GetException()?.Message}");
			ErrorLog.Warn($"Error with SettingsFileWatcher: {e.GetException()?.Message}");
		}


	}

	
}
