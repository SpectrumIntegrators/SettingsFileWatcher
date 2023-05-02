using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Crestron.SimplSharp;
using System.Timers;

namespace SFW {
	/// <summary>
	/// Watches a file and reports when it has been changed (or created).
	/// Since multiple change events may be fired for one "change" procedure,
	/// there is a hesitation delay that causes all of the intermediate change
	/// events to only issue one event from this class.
	/// </summary>
	public class SettingsFileWatcher {
		private const int HESITATION_MS = 1000;
		private string _FilePath;
		private bool _Debug = false;
		private FileSystemWatcher _FileWatcher = null;
		private Timer _Timer = new Timer(HESITATION_MS);

		public delegate void FileChangedHandler(object sender, EventArgs e);
		/// <summary>
		/// This event is raised when the file has changed and the hesitation delay has elapsed.
		/// </summary>
		public event FileChangedHandler FileChanged;

		/// <summary>
		/// Prints a TRACE message to the Crestron console if debug mode has been enabled
		/// </summary>
		/// <param name="msg">The message to print</param>
		private void _dbg(string msg) {
			if (this._Debug) {
				CrestronConsole.Print("\x00FA\x00E0{0}{1}\x00FB", (char)InitialParametersClass.ApplicationNumber, msg);
			}
		}
		/// <summary>
		/// Prints a TRACE message to the Crestron console if debug mode has been enabled
		/// </summary>
		/// <param name="msg">The message format string</param>
		/// <param name="args">Values to use in the message</param>
		private void _dbg(string msg, params object[] args) {
			if (this._Debug) {
				_dbg(string.Format(msg, args));
			}
		}

		/// <summary>
		/// Start watching the specified file for changes
		/// </summary>
		/// <param name="FilePath">The full path and filename of the file to watch</param>
		public void Init(string FilePath) {
			_dbg($"Initializing SettingsFileWatcher for file {FilePath}");
			if (this._FileWatcher != null) {
				// If we're re-initializing, get rid of any old watcher
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

			// Initialize file watcher
			this._FileWatcher.Filter = Path.GetFileName(FilePath);
			this._FileWatcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.LastWrite;
			this._FileWatcher.Changed += _FileChanged;
			this._FileWatcher.Created += _FileChanged;
			this._FileWatcher.IncludeSubdirectories = false;
			this._FileWatcher.EnableRaisingEvents = true;

			// Initialize hesitation timer
			this._Timer.AutoReset = false;
			this._Timer.Elapsed += _Timer_Elapsed;

		}

		/// <summary>
		/// Enables or disables debug message printing
		/// </summary>
		/// <param name="Debug">Enables debug message printing if true</param>
		public void SetDebug(uint Debug) {
			this._Debug = (Debug == 0 ? false : true);
		}

		private void _FileChanged(object sender, FileSystemEventArgs e) {
			_dbg($"File {e.FullPath} has changed");
			// Restart the hesitation timer
			this._Timer.Stop();
			this._Timer.Start();
		}

		private void _Timer_Elapsed(object sender, ElapsedEventArgs e) {
			// The file has changed, but the hesitation delay has passed so report the change
			FileChanged(this, new EventArgs());
		}

		private void _FileError(object sender, ErrorEventArgs e) {
			_dbg($"Error with SettingsFileWatcher: {e.GetException()?.Message}");
			ErrorLog.Warn($"Error with SettingsFileWatcher: {e.GetException()?.Message}");
		}


	}

	
}
