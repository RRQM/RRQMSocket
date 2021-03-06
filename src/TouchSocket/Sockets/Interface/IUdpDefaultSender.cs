using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TouchSocket.Core.ByteManager;

namespace TouchSocket.Sockets
{
    /// <summary>
    /// 具有直接发送功能
    /// </summary>
    public interface IUdpDefaultSender : ISendBase
    {
        #region 默认发送
        /// <summary>
        /// 绕过适配器，直接发送字节流
        /// </summary>
        /// <param name="endPoint">目的终结点</param>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="length">数据长度</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        void DefaultSend(EndPoint endPoint, byte[] buffer, int offset, int length);

        /// <summary>
        /// 绕过适配器，直接发送字节流
        /// </summary>
        /// <param name="endPoint">目的终结点</param>
        /// <param name="buffer">数据缓存区</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        void DefaultSend(EndPoint endPoint, byte[] buffer);

        /// <summary>
        /// 绕过适配器，直接发送字节流
        /// </summary>
        /// <param name="endPoint">目的终结点</param>
        /// <param name="byteBlock">数据块载体</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        void DefaultSend(EndPoint endPoint, ByteBlock byteBlock);
        #endregion

        #region 默认发送
        /// <summary>
        /// 绕过适配器，直接发送字节流
        /// </summary>
        /// <param name="endPoint">目的终结点</param>
        /// <param name="buffer">数据缓存区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="length">数据长度</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        void DefaultSendAsync(EndPoint endPoint, byte[] buffer, int offset, int length);

        /// <summary>
        /// 绕过适配器，直接发送字节流
        /// </summary>
        /// <param name="endPoint">目的终结点</param>
        /// <param name="buffer">数据缓存区</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        void DefaultSendAsync(EndPoint endPoint, byte[] buffer);

        /// <summary>
        /// 绕过适配器，直接发送字节流
        /// </summary>
        /// <param name="endPoint">目的终结点</param>
        /// <param name="byteBlock">数据块载体</param>
        /// <exception cref="NotConnectedException">客户端没有连接</exception>
        /// <exception cref="OverlengthException">发送数据超长</exception>
        /// <exception cref="Exception">其他异常</exception>
        void DefaultSendAsync(EndPoint endPoint, ByteBlock byteBlock);
        #endregion
    }
}
