﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Edda;

public class NoteScanner {
    int noteScanIndex;
    int noteScanStopwatchOffset = 0;
    Stopwatch noteScanStopwatch;
    CancellationTokenSource noteScanTokenSource;
    CancellationToken noteScanToken;

    double globalBPM;
    public List<Note> notes;

    MainWindow caller;
    DrumPlayer drummer;

    public NoteScanner(MainWindow caller, DrumPlayer drummer) {
        this.drummer = drummer;
        this.noteScanStopwatch = new Stopwatch();
        this.caller = caller;
    }

    public void Start(int millisecStart, List<Note> notes, double globalBPM) {
        this.globalBPM = globalBPM;
        this.notes = notes;
        noteScanStopwatchOffset = millisecStart; // set user audio delay
        SetScanStart();

        // start scanning for notes
        noteScanTokenSource = new CancellationTokenSource();
        noteScanToken = noteScanTokenSource.Token;
        Task.Run(() => BeginScan(noteScanStopwatchOffset, noteScanToken), noteScanToken);

        noteScanStopwatch.Start();
    }
    public void Stop() {
        if (noteScanTokenSource != null) {
            noteScanTokenSource.Cancel();
        }
        noteScanStopwatch.Reset();
    }
    private void SetScanStart() {
        // calculate scan index for playing drum hits
        var seekBeat = noteScanStopwatchOffset * globalBPM / 60000;
        var newNoteScanIndex = 0;
        foreach (var n in notes) {
            if (Helper.DoubleApproxGreaterEqual(n.beat, seekBeat)) {
                break;
            }
            newNoteScanIndex++;
        }
        noteScanIndex = newNoteScanIndex;
    }
    private void BeginScan(int startFrom, CancellationToken ct) {
        // NOTE: this function is called on a separate thread

        // scan notes while song is still playing
        var nextPollTime = Const.Audio.NotePollRate;
        while (!ct.IsCancellationRequested) {
            if (noteScanStopwatch.ElapsedMilliseconds + startFrom >= nextPollTime) {
                ScanNotes();
                nextPollTime += Const.Audio.NotePollRate;
            }
        }
    }
    private void ScanNotes() {
        var currentTime = noteScanStopwatch.ElapsedMilliseconds + noteScanStopwatchOffset;
        // check if we started past the last note in the song
        var noteCols = new List<int>();
        var notesPlayed = new List<Note>();
        if (noteScanIndex < notes.Count) {
            var noteTime = 60000 * notes[noteScanIndex].beat / globalBPM;
            var drumHits = 0;

            // check if any notes were missed
            while (currentTime - noteTime >= Const.Audio.NoteDetectionDelta && noteScanIndex < notes.Count - 1) {
                Trace.WriteLine($"WARNING: A note was played late during playback. (Delta: {Math.Round(currentTime - noteTime, 2)})");
                
                drumHits++;
                noteCols.Add(notes[noteScanIndex].col);
                notesPlayed.Add(notes[noteScanIndex]);
                noteScanIndex++;
                noteTime = 60000 * notes[noteScanIndex].beat / globalBPM;
            }

            // check if we need to play any notes
            while (Math.Abs(currentTime - noteTime) < Const.Audio.NoteDetectionDelta) {
                //Trace.WriteLine($"Played note at beat {selectedDifficultyNotes[noteScanIndex].Item1}");

                drumHits++;
                noteCols.Add(notes[noteScanIndex].col);
                notesPlayed.Add(notes[noteScanIndex]);
                noteScanIndex++;
                if (noteScanIndex >= notes.Count) {
                    break;
                }
                noteTime = 60000 * notes[noteScanIndex].beat / globalBPM;
            }

            // play all pending drum hits
            if (drummer.PlayDrum(drumHits) == false) {
                Trace.WriteLine("WARNING: Drummer skipped a drum hit");
            }

            //foreach (int c in noteCols) {
            //    caller.Dispatcher.Invoke(() => {
            //        caller.AnimateDrum(c);
            //    });
            //}
            foreach (Note n in notesPlayed) {
                caller.Dispatcher.Invoke(() => {
                    caller.AnimateNote(n);
                });
            }
        }
    }
}
