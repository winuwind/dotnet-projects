using System.Collections.Concurrent;
using System.Net.Sockets;

namespace SOCKS_Proxy.Control;

public class Selector(Server server)
{
    private readonly ConcurrentDictionary<Socket, ConcurrentQueue<TaskRead>> _readTasks = new();
    private readonly ConcurrentDictionary<Socket, ConcurrentQueue<TaskWrite>> _writeTasks = new();
    private readonly Server _server = server;
    
    private bool _isRunning = true;

    public void AddTaskRead(Socket socket, TaskRead task)
    {
        if (!_isRunning)
        {
            task.Tcs.SetResult(0);
            return;
        }
        if (_readTasks.TryGetValue(socket, out var readTask))
        {
            readTask.Enqueue(task);
        }
        else
        {
            _readTasks[socket] = new ConcurrentQueue<TaskRead>();
            _readTasks[socket].Enqueue(task);
        }
    }

    public void AddTaskWrite(Socket socket, TaskWrite task)
    {
        if (!_isRunning)
        {
            task.Tcs.SetResult(0);
            return;
        }
        if (_writeTasks.TryGetValue(socket, out var writeTask))
        {
            writeTask.Enqueue(task);
        }
        else
        {
            _writeTasks[socket] = new ConcurrentQueue<TaskWrite>();
            _writeTasks[socket].Enqueue(task);
        }
    }

    public void Work()
    {
        while (_isRunning)
        {
            var socketsRead = _readTasks.Keys.ToList();
            var socketsWrite = _writeTasks.Keys.ToList();
            var socketsError = socketsRead.Concat(socketsWrite).ToList();
            
            socketsRead.Add(_server.GetListener());

            try
            {
                Socket.Select(socketsRead, socketsWrite, socketsError, 100);
            }
            catch
            {
                foreach (var socket in _readTasks.Keys)
                {
                    if (socket.SafeHandle.IsClosed)
                    {
                        CloseSocket(socket);
                        _readTasks.TryRemove(socket, out _);
                    }
                }
                foreach (var socket in _writeTasks.Keys)
                {
                    if (socket.SafeHandle.IsClosed)
                    {
                        CloseSocket(socket);
                        _writeTasks.TryRemove(socket, out _);
                    }
                }
                continue;
            }
            
            foreach (var socket in socketsRead)
            {
                if (Equals(socket, _server.GetListener()))
                {
                    _server.Accept();
                }
                else
                {
                    CompleteRead(socket);
                }
            }

            foreach (var socket in socketsWrite)
            {
                CompleteWrite(socket);
            }

            foreach (var socket in socketsError)
            {
                CloseSocket(socket);
                _readTasks.TryRemove(socket, out _);
                _writeTasks.TryRemove(socket, out _);
            }
        }

        foreach (var readTasksList in _readTasks.Values)
        {
            foreach (var readTask in readTasksList)
            {
                readTask.Tcs.SetResult(0);
            }
        }

        foreach (var writeTasksList in _writeTasks.Values)
        {
            foreach (var writeTask in writeTasksList)
            {
                writeTask.Tcs.SetResult(0);
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
    }

    private void CloseSocket(Socket socket)
    {
        if (_readTasks.TryGetValue(socket, out var readList))
        {
            foreach (var taskRead in readList)
            {
                taskRead.Tcs.SetResult(0);
            }
            readList.Clear();
        }

        if (_writeTasks.TryGetValue(socket, out var writeList))
        {
            foreach (var taskWrite in writeList)
            {
                taskWrite.Tcs.SetResult(0);
            }
            writeList.Clear();
        }
    }

    private void CompleteRead(Socket socket)
    {
        if (!_readTasks.TryGetValue(socket, out var readList) || readList.Count == 0)
        {
            _readTasks.TryRemove(socket, out _);
            return;
        }
        
        if (_readTasks[socket].TryPeek(out var task))
        {
            var bytesRead = 0;
        
            try
            {
                if (task.EndPoint != null)
                {
                    bytesRead = socket.ReceiveFrom(task.Buffer, task.Offset, task.Count, SocketFlags.None, ref task.EndPoint);
                }
                else
                {
                    bytesRead = socket.Receive(task.Buffer, task.Offset, task.Count, SocketFlags.None);
                }
            }
            catch
            {
                bytesRead = 0;
            }
            finally
            {
                task.Count -= bytesRead;
                if (task.Count > 0 && task.FlagNeedFull && bytesRead > 0)
                {
                    task.Offset += bytesRead;
                }
                else
                {
                    _readTasks[socket].TryDequeue(out _);
                    if (bytesRead <= 0)
                    {
                        CloseSocket(socket);
                        _readTasks.TryRemove(socket, out _);
                    }
                    else if (_readTasks[socket].Count == 0)
                    {
                        _readTasks.TryRemove(socket, out _);
                        task.Tcs.SetResult(bytesRead);
                    }
                    else
                    {
                        task.Tcs.SetResult(bytesRead);
                    }
                }
            }
        }
    }


    private void CompleteWrite(Socket socket)
    {
        if (!_writeTasks.TryGetValue(socket, out var writeList) || writeList.Count == 0)
        {
            _writeTasks.TryRemove(socket, out _);
            return;
        }
        
        if (_writeTasks[socket].TryPeek(out var task))
        {
            var bytesSend = 0;
            try
            {
                if (task.EndPoint != null)
                {
                    bytesSend = socket.SendTo(task.Buffer, task.Offset, task.Count, SocketFlags.None, task.EndPoint);
                }
                else
                {
                    bytesSend = socket.Send(task.Buffer, task.Offset, task.Count, SocketFlags.None);
                }
            }
            catch
            {
                bytesSend = 0;
            }
            finally
            {
                task.Count -= bytesSend;
                if (task.Count > 0 && bytesSend > 0)
                {
                    task.Offset += bytesSend;
                }
                else
                {
                    _writeTasks[socket].TryDequeue(out _);

                    if (bytesSend <= 0)
                    {
                        CloseSocket(socket);
                        _writeTasks.TryRemove(socket, out _);
                    }

                    else if (_writeTasks[socket].Count == 0)
                    {
                        _writeTasks.TryRemove(socket, out _);
                        task.Tcs.SetResult(bytesSend);
                    }
                    else
                    {
                        task.Tcs.SetResult(bytesSend);
                    }
                }
            }
        }
    }
}