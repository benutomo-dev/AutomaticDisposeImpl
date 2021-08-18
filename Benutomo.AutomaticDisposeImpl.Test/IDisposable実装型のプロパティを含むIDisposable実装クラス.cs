using Benutomo.AutomaticDisposeImpl.Test.TestUtils;
using FluentAssertions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace Benutomo.AutomaticDisposeImpl.Test
{
    public partial class IDisposable�����^�̃v���p�e�B���܂�IDisposable�����N���X
    {
        [AutomaticDisposeImpl]
        partial class NullPropertyClass : IDisposable
        {
            internal IDisposable disposable { get; set; } = null;
        }

        [AutomaticDisposeImpl(ReleaseUnmanagedResourcesMethod = nameof(SelfImplFinalize), SelfDisposeMethod = nameof(SelfImplSyncDispose))]
        partial class ExclusivityTestBaseClass : IDisposable
        {
            internal ImplicitDisposableImplementClass baseDisposable { get; set; } = new();

            int baseImplReleaseUnmanagedResourceCallCount;
            int baseImplDisposeCallCount;

            public int BaseImplReleaseUnmanagedResourceCallCount => Thread.VolatileRead(ref baseImplReleaseUnmanagedResourceCallCount);
            public int BaseImplDisposeCallCount => Thread.VolatileRead(ref baseImplDisposeCallCount);

            void SelfImplFinalize()
            {
                Interlocked.Increment(ref baseImplReleaseUnmanagedResourceCallCount);
            }

            void SelfImplSyncDispose()
            {
                Interlocked.Increment(ref baseImplDisposeCallCount);
            }
        }

        [AutomaticDisposeImpl(ReleaseUnmanagedResourcesMethod = nameof(SelfImplFinalize), SelfDisposeMethod = nameof(SelfImplSyncDispose))]
        partial class ExclusivityTestClass : ExclusivityTestBaseClass
        {
            internal ImplicitDisposableImplementClass selfDisposable { get; set; } = new();

            int selfImplReleaseUnmanagedResourceCallCount;
            int selfImplDisposeCallCount;

            public int SelfImplReleaseUnmanagedResourceCallCount => Thread.VolatileRead(ref selfImplReleaseUnmanagedResourceCallCount);
            public int SelfImplDisposeCallCount => Thread.VolatileRead(ref selfImplDisposeCallCount);

            void SelfImplFinalize()
            {
                Interlocked.Increment(ref selfImplReleaseUnmanagedResourceCallCount);
            }

            void SelfImplSyncDispose()
            {
                Interlocked.Increment(ref selfImplDisposeCallCount);
            }
        }

        [AutomaticDisposeImpl]
        partial class GetonlyPropertyClass : IDisposable
        {
            internal IDisposable disposable { get; }

            public GetonlyPropertyClass(IDisposable disposable)
            {
                this.disposable = disposable;
            }
        }

        [AutomaticDisposeImpl]
        partial class GenericTypePropertyClass<T> : IDisposable where T : IDisposable
        {
            internal T disposable { get; }

            public GenericTypePropertyClass(T disposable)
            {
                this.disposable = disposable;
            }
        }

        [AutomaticDisposeImpl]
        partial class InterfacePropertyClass : IDisposable
        {
            internal IDisposable disposable { get; set; }

            public InterfacePropertyClass(IDisposable disposable)
            {
                this.disposable = disposable;
            }
        }

        [AutomaticDisposeImpl]
        partial class ImplicitDisposableImplementClassPropertyClass : IDisposable
        {
            internal ImplicitDisposableImplementClass disposable { get; set; } = new();
        }

        [AutomaticDisposeImpl]
        partial class ExplicitDisposableImplemetnClassPropertyClass : IDisposable
        {
            internal ExplicitDisposableImplemetnClass disposable { get; set; } = new();
        }

        [Fact]
        public void �v���p�e�B��null�l�ƂȂ��Ă���ꍇ�ł�Dispose�ŗ�O�͔������Ȃ�()
        {
            var testeeObject = new NullPropertyClass();
            testeeObject.Dispose();
        }

        [Fact]
        public void Dispose�������X���b�h���瓯���ɌĂяo����Ă��ŏ��̌Ăяo���݂̂��r���I�ɗL���ƂȂ�()
        {
            const int millisecondsTimeout = 1000;

            var testIterationCount = Math.Min(10, Environment.ProcessorCount * 3);

            for (int i = 0; i < testIterationCount; i++)
            {
                // �O�̂��߁A�������s����ύX���Ȃ��畡���񔽕����ă`�F�b�N����
                var disposeCallThreadCount = 1 + i;


                using var disposeStartEvent = new ManualResetEventSlim(false);
                using var disposeEnteredEvent = new ManualResetEventSlim(false);
                using var disposeBlockEvent = new ManualResetEventSlim(false);

                var testeeObject = new ExclusivityTestClass();

                var unsetCountAtBaseDisposableMemberDisposeCalling = 0;

                testeeObject.baseDisposable.OnDispose = () =>
                {
                    if (!disposeBlockEvent.IsSet)
                    {
                        // �e�N���X�̃����o�[�̔j���͔h���N���X�̃����o�[�̔j��������ɍs���邽��
                        // �����������ɂ����āA���̃^�C�~���O��disposeBlockEvent.IsSet���U�ƂȂ邱�Ƃ͂Ȃ��͂��B
                        Interlocked.Increment(ref unsetCountAtBaseDisposableMemberDisposeCalling);
                    }
                };

                testeeObject.selfDisposable.OnDispose = () =>
                {
                    disposeEnteredEvent.Set();
                    disposeBlockEvent.Wait(millisecondsTimeout);
                };

                var threads = Enumerable.Range(0, disposeCallThreadCount).Select(id => new Thread(_ =>
                {
                    disposeStartEvent.Wait(millisecondsTimeout);
                    testeeObject.Dispose();
                })).ToArray();

                foreach (var thread in threads)
                {
                    thread.Start();
                }

                testeeObject.BaseImplDisposeCallCount.Should().Be(0, "�܂�disposeStartEvent���Z�b�g����Ă��Ȃ��̂ŁADispose�͌Ă΂�Ă��Ȃ��͂��B");
                testeeObject.SelfImplDisposeCallCount.Should().Be(0, "�܂�disposeStartEvent���Z�b�g����Ă��Ȃ��̂ŁADispose�͌Ă΂�Ă��Ȃ��͂��B");

                testeeObject.baseDisposable.ManagedContextDisposeCount.Should().Be(0, "�܂�disposeStartEvent���Z�b�g����Ă��Ȃ��̂ŁADispose�͌Ă΂�Ă��Ȃ��͂��B");
                testeeObject.selfDisposable.ManagedContextDisposeCount.Should().Be(0, "�܂�disposeStartEvent���Z�b�g����Ă��Ȃ��̂ŁADispose�͌Ă΂�Ă��Ȃ��͂��B");

                testeeObject.IsDisposed.Should().BeFalse("�܂�disposeStartEvent���Z�b�g����Ă��Ȃ��̂ŁADispose�͌Ă΂�Ă��Ȃ��͂��B");

                disposeStartEvent.Set();
                // �����X���b�h��������Dispose���J�n

                disposeEnteredEvent.Wait(millisecondsTimeout);
                // �����ꂩ�̃X���b�h����̌Ăяo���������o��Dispose�ɓ��B

                testeeObject.BaseImplDisposeCallCount.Should().Be(0, "�܂������o��Dispose�̒��Ŕj���̐i�s���u���b�N����Ă���̂ŃJ�E���g�͕ω����Ȃ��͂��B");
                testeeObject.SelfImplDisposeCallCount.Should().Be(0, "�܂������o��Dispose�̒��Ŕj���̐i�s���u���b�N����Ă���̂ŃJ�E���g�͕ω����Ȃ��͂��B");

                testeeObject.selfDisposable.ManagedContextDisposeCount.Should().Be(0, "�܂�Dispose�̓����Ńu���b�N����Ă���̂ŃJ�E���g�͕ω����Ȃ��͂��B");
                testeeObject.IsDisposed.Should().BeTrue("�����Ŋ��ɔr���I��Dispose���J�n���ꂽ���_��Dispose�����O�ɐ^�ƂȂ�͂��B");

                disposeBlockEvent.Set();
                // �����o��Dispose�̃u���b�N������

                foreach (var thread in threads)
                {
                    thread.Join(millisecondsTimeout);
                }

                testeeObject.BaseImplDisposeCallCount.Should().Be(1, "�����X���b�h���瓯����Dispose���Ăяo����Ă��Ă������҂�Dispose�͍ŏ��̂P��ɑ΂��Ă̂ݔr���I�ɌĂяo�����͂��B");
                testeeObject.SelfImplDisposeCallCount.Should().Be(1, "�����X���b�h���瓯����Dispose���Ăяo����Ă��Ă������҂�Dispose�͍ŏ��̂P��ɑ΂��Ă̂ݔr���I�ɌĂяo�����͂��B");

                testeeObject.BaseImplReleaseUnmanagedResourceCallCount.Should().Be(1, "�����X���b�h���瓯����Dispose���Ăяo����Ă��Ă������҂�ReleaseUnmanagedResource�͍ŏ��̂P��ɑ΂��Ă̂ݔr���I�ɌĂяo�����͂��B");
                testeeObject.SelfImplReleaseUnmanagedResourceCallCount.Should().Be(1, "�����X���b�h���瓯����Dispose���Ăяo����Ă��Ă������҂�ReleaseUnmanagedResource�͍ŏ��̂P��ɑ΂��Ă̂ݔr���I�ɌĂяo�����͂��B");

                testeeObject.baseDisposable.ManagedContextDisposeCount.Should().Be(1, "�����X���b�h���瓯����Dispose���Ăяo����Ă��Ă������o��Dispose�͍ŏ��̂P��ɑ΂��Ă̂ݔr���I�ɌĂяo�����͂��B");
                testeeObject.selfDisposable.ManagedContextDisposeCount.Should().Be(1, "�����X���b�h���瓯����Dispose���Ăяo����Ă��Ă������o��Dispose�͍ŏ��̂P��ɑ΂��Ă̂ݔr���I�ɌĂяo�����͂��B");

                testeeObject.baseDisposable.UnmanagedContextDisposeCount.Should().Be(1, "�����X���b�h���瓯����Dispose���Ăяo����Ă��Ă������o��Dispose�͍ŏ��̂P��ɑ΂��Ă̂ݔr���I�ɌĂяo�����͂��B");
                testeeObject.selfDisposable.UnmanagedContextDisposeCount.Should().Be(1, "�����X���b�h���瓯����Dispose���Ăяo����Ă��Ă������o��Dispose�͍ŏ��̂P��ɑ΂��Ă̂ݔr���I�ɌĂяo�����͂��B");

                unsetCountAtBaseDisposableMemberDisposeCalling.Should().Be(0, "�e�N���X�̃����o��Dispose���h���N���X�̃����o��Dispose����ɌĂяo����Ă��Ȃ����0�̂͂��B");
            }
        }

        [Fact]
        public void getonly�v���p�e�B�ɑ΂��鎩������()
        {
            var disposableMock = new Mock<IDisposable>(MockBehavior.Strict);

            disposableMock.Setup(v => v.Dispose());

            var testeeObject = new GetonlyPropertyClass(disposableMock.Object);

            testeeObject.Dispose();
            disposableMock.Verify(v => v.Dispose(), Times.Once(), "Dispose�̌Ăяo�����`�����Ă��Ȃ��B");

            testeeObject.Dispose();
            disposableMock.Verify(v => v.Dispose(), Times.Once(), "�Q��ڈȍ~��Dispose�̌Ăяo�����`�����Ă��܂��Ă���B");
        }

        [Fact]
        public void �W�F�l���b�N�^�v���p�e�B�ɑ΂��鎩������()
        {
            var disposableMock = new Mock<IDisposable>(MockBehavior.Strict);

            disposableMock.Setup(v => v.Dispose());

            var testeeObject = new GenericTypePropertyClass<IDisposable>(disposableMock.Object);

            testeeObject.Dispose();
            disposableMock.Verify(v => v.Dispose(), Times.Once(), "Dispose�̌Ăяo�����`�����Ă��Ȃ��B");

            testeeObject.Dispose();
            disposableMock.Verify(v => v.Dispose(), Times.Once(), "�Q��ڈȍ~��Dispose�̌Ăяo�����`�����Ă��܂��Ă���B");
        }

        [Fact]
        public void IDisposable�^�̃v���p�e�B�ɑ΂��鎩������()
        {
            var disposableMock = new Mock<IDisposable>(MockBehavior.Strict);

            disposableMock.Setup(v => v.Dispose());

            var testeeObject = new InterfacePropertyClass(disposableMock.Object);

            testeeObject.Dispose();
            disposableMock.Verify(v => v.Dispose(), Times.Once(), "Dispose�̌Ăяo�����`�����Ă��Ȃ��B");

            testeeObject.Dispose();
            disposableMock.Verify(v => v.Dispose(), Times.Once(), "�Q��ڈȍ~��Dispose�̌Ăяo�����`�����Ă��܂��Ă���B");
        }

        [Fact]
        public void IDisposable�𒼐ڎ������Ă���^�̃v���p�e�B�ɑ΂��鎩������()
        {
            var testeeObject = new ImplicitDisposableImplementClassPropertyClass();

            testeeObject.Dispose();
            testeeObject.disposable.ManagedContextDisposeCount.Should().Be(1, "�ŏ���Dispose�̌Ăяo���͓`������Ȃ���΂Ȃ�Ȃ��B");

            testeeObject.Dispose();
            testeeObject.disposable.ManagedContextDisposeCount.Should().Be(1, "�Q��ڈȍ~��Dispose�̌Ăяo�����`�����Ă͂����Ȃ��B");
        }

        [Fact]
        public void IDisposable�𒼐ږ����I�Ɏ������Ă���^�̃v���p�e�B�ɑ΂��鎩������()
        {
            var testeeObject = new ExplicitDisposableImplemetnClassPropertyClass();

            testeeObject.Dispose();
            testeeObject.disposable.ManagedContextDisposeCount.Should().Be(1, "�ŏ���Dispose�̌Ăяo���͓`������Ȃ���΂Ȃ�Ȃ��B");

            testeeObject.Dispose();
            testeeObject.disposable.ManagedContextDisposeCount.Should().Be(1, "�Q��ڈȍ~��Dispose�̌Ăяo�����`�����Ă͂����Ȃ��B");
        }
    }
}
