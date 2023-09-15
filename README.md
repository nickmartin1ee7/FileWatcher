# FileWatcher

## Description

This application will monitor a directory and dispatch worker threads to read, process, and move files as quickly as possible.

## Process Flow

1. FileWatcher uses events to enqueue files that were created to a queue.
2. MonitorService rallies workers (threads) to process files in the queue as they are added.
3. MonitorService ensures required folders are created and read/writeable.
4. MonitorService also scans the Input directory on a configurable interval and adds those file that are "truant"/late to the queue.
5. Worker(s) dispatch the FileHandler to handle the dequeued file.
6. FileHandler will instruct the IProcessor to process the file, then if successful move it to the Output directory.
7. Worker(s) will requeue IO-related failures when trying to process a file.
8. MonitorService will renqueue truant files that had failed to be processed.