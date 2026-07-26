using System;
using NUnit.Framework;
using EnglishKingdom.SaveSystem;

namespace EnglishKingdom.Tests.SaveSystem
{
    [TestFixture]
    public class ExceptionTests
    {
        // ── SaveException ─────────────────────────────────────────────────────────

        [Test]
        public void SaveException_StoresErrorCode()
        {
            var ex = new SaveException(SaveErrorCode.StorageError, "msg");

            Assert.AreEqual(SaveErrorCode.StorageError, ex.ErrorCode);
        }

        [Test]
        public void SaveException_StoresMessage()
        {
            var ex = new SaveException(SaveErrorCode.NetworkError, "some message");

            Assert.AreEqual("some message", ex.Message);
        }

        [Test]
        public void SaveException_WithInnerException_StoresInnerException()
        {
            var inner = new InvalidOperationException("inner");
            var ex = new SaveException(SaveErrorCode.SerializationError, "outer", inner);

            Assert.AreSame(inner, ex.InnerException);
            Assert.AreEqual(SaveErrorCode.SerializationError, ex.ErrorCode);
        }

        [Test]
        public void SaveException_InnerExceptionDefaultsToNull()
        {
            var ex = new SaveException(SaveErrorCode.AuthError, "auth failed");

            Assert.IsNull(ex.InnerException);
        }

        // ── ForwardCompatibilityException ─────────────────────────────────────────

        [Test]
        public void ForwardCompatibilityException_StoresSavedVersion()
        {
            var ex = new ForwardCompatibilityException(savedVersion: 5, latestVersion: 2);

            Assert.AreEqual(5, ex.SavedVersion);
        }

        [Test]
        public void ForwardCompatibilityException_StoresLatestVersion()
        {
            var ex = new ForwardCompatibilityException(savedVersion: 5, latestVersion: 2);

            Assert.AreEqual(2, ex.LatestVersion);
        }

        [Test]
        public void ForwardCompatibilityException_MessageContainsBothVersionNumbers()
        {
            var ex = new ForwardCompatibilityException(savedVersion: 5, latestVersion: 2);

            StringAssert.Contains("5", ex.Message);
            StringAssert.Contains("2", ex.Message);
        }

        [Test]
        public void ForwardCompatibilityException_IsException()
        {
            var ex = new ForwardCompatibilityException(3, 1);

            Assert.IsInstanceOf<Exception>(ex);
        }
    }
}
