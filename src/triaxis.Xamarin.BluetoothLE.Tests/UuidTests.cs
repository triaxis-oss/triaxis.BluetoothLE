using NUnit.Framework;

namespace triaxis.Xamarin.BluetoothLE.Tests
{
    public class UuidTests
    {
        [Test]
        public void ConstructorFromSegments()
        {
            Assert.AreEqual(new Uuid("01234567-89AB-CDEF-FEDC-BA9876543210"),
                new Uuid(0x01234567, 0x89AB, 0xCDEF, 0xFEDC, 0xBA9876543210));
        }

        [Test]
        public void ConstructorFromHalves()
        {
            Assert.AreEqual(new Uuid("01234567-89AB-CDEF-FEDC-BA9876543210"),
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210));
        }

        [Test]
        public void ConstructorFrom16Bit()
        {
            Assert.AreEqual(new Uuid("00002A01-0000-1000-8000-00805F9B34FB"),
                new Uuid(0x2A01));
        }

        [Test]
        public void ConstructorFrom32Bit()
        {
            Assert.AreEqual(new Uuid("80002A01-0000-1000-8000-00805F9B34FB"),
                new Uuid(0x80002A01));
        }

        [Test]
        public void ConstructorFromString()
        {
            Assert.AreEqual(new Uuid("01234567-89AB-CDEF-FEDC-BA9876543210"),
                new Uuid("0123456789ABCDEFFEDCBA9876543210"));
            Assert.AreEqual(new Uuid("01234567-89AB-CDEF-FEDC-BA9876543210"),
                new Uuid("012z34w56g78y9A:BC+DE-F..FED  CBA/98=76543210"));
        }

        [Test]
        public void FromLE()
        {
            Assert.AreEqual(new Uuid("00112233-4455-6677-8899-AABBCCDDEEFF"),
                Uuid.FromLE(new byte[] { 0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA, 0x99, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11, 0x00 }));
        }

        [Test]
        public void FromBE()
        {
            Assert.AreEqual(new Uuid("00112233-4455-6677-8899-AABBCCDDEEFF"),
                Uuid.FromBE(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }));
        }

        [Test]
        public void ToLE()
        {
            Assert.AreEqual(new Uuid("00112233-4455-6677-8899-AABBCCDDEEFF").ToByteArrayLE(),
                new byte[] { 0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA, 0x99, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11, 0x00 });
        }

        [Test]
        public void ToBE()
        {
            Assert.AreEqual(new Uuid("00112233-4455-6677-8899-AABBCCDDEEFF").ToByteArrayBE(),
                new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF });
        }

        [Test]
        public void CompareLeftHalf()
        {
            Assert.That(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210).CompareTo(
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210)), Is.EqualTo(0));
            Assert.That(new Uuid(0x0123456789ABCDEE, 0xFEDCBA9876543210).CompareTo(
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210)), Is.LessThan(0));
            Assert.That(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210).CompareTo(
                new Uuid(0x0123456789ABCDEE, 0xFEDCBA9876543210)), Is.GreaterThan(0));
        }

        [Test]
        public void CompareRightHalf()
        {
            Assert.That(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210).CompareTo(
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210)), Is.EqualTo(0));
            Assert.That(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210).CompareTo(
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543211)), Is.LessThan(0));
            Assert.That(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543211).CompareTo(
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210)), Is.GreaterThan(0));
        }

        [Test]
        public void Equality()
        {
            Assert.IsTrue(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210).Equals(
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210)));
            Assert.IsTrue(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210) ==
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210));
            Assert.IsFalse(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210) !=
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210));
        }

        [Test]
        public void Inequality()
        {
            Assert.IsFalse(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210).Equals(
                new Uuid(0xFEDCBA9876543210, 0x0123456789ABCDEF)));
            Assert.IsFalse(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210) ==
                new Uuid(0xFEDCBA9876543210, 0x0123456789ABCDEF));
            Assert.IsTrue(new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210) !=
                new Uuid(0xFEDCBA9876543210, 0x0123456789ABCDEF));
        }

        [Test]
        public void ConversionToString()
        {
            Assert.AreEqual("01234567-89AB-CDEF-FEDC-BA9876543210",
                new Uuid(0x0123456789ABCDEF, 0xFEDCBA9876543210).ToString());
        }
    }
}
