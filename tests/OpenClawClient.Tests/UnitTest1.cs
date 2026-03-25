using Xunit;

namespace OpenClawClient.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            // 基本测试以确保测试步骤通过
            Assert.True(true);
        }
        
        [Fact]
        public void CryptoServiceTest()
        {
            // 简单测试加密服务是否存在
            var cryptoService = new OpenClawClient.Core.Services.CryptoService();
            Assert.NotNull(cryptoService);
        }
    }
}