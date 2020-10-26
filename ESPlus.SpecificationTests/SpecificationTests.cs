using System;
using System.Collections.Generic;
using ESPlus.Specification;
using Xunit;

namespace ESPlus.SpecificationTests
{
    public class SpecificationTests : Specification<DummyAggregate>
    {
        [Theory]
        [InlineData("Given", null)]
        [InlineData("Given When", null)]
        [InlineData("Given When Then", null)]
        [InlineData("Given Then", null)]
        [InlineData("Given Given", "Given has already been specified")]
        [InlineData("Given Then Then", "Then has already been specified")]
        [InlineData("Given When When", "When has already been specified")]
        [InlineData("Given Then When", "When has to be specified before Then")]
        [InlineData("When", "Given has to be specified before When")]
        [InlineData("Then", "Given has to be specified before Then")]
        public void Theory_MixGivenWhenThenOrder_Expected(string operations, string expectedExceptionMessage)
        {
            Exception exception = null;

            try
            {
                var map = new Dictionary<string, Action>
                {
                    ["Given"] = () => Given(() => Aggregate = new DummyAggregate("id")),
                    ["When"] = () => When(() => { }),
                    ["Then"] = () => Then(() => { }),
                };

                foreach (var operation in operations.Split())
                {
                    map[operation]();
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Mute();
            Assert.Equal(expectedExceptionMessage, exception?.Message);
        }

        [Fact]
        public void No_aggregate_was_specified_in_Given()
        {
            var exception = Assert.Throws<Exception>(() => Given(() => { }));

            Assert.Equal("No Aggregate wasn't assigned in Given", exception.Message);
        }

        [Fact]
        public void Dispose_without_ever_calling_Given()
        {
            var exception = Assert.Throws<Exception>(() => Dispose());

            Mute();
            Assert.Equal("Given was never set", exception.Message);
        }

        [Fact]
        public void Dispose_without_ever_calling_Then()
        {
            Given(() =>
            {
                Aggregate = new DummyAggregate("id");
            });

            var exception = Assert.Throws<Exception>(() => Dispose());

            Mute();
            Assert.Equal("Then was never set", exception.Message);
        }

        [Fact]
        public void Throws_within_Given()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                Given(() =>
                {
                    Aggregate = new DummyAggregate("id");
                    throw new Exception("Throws in given");
                });

                Dispose();
            });

            Mute();
            Assert.Equal("Throws in given", exception.Message);
        }

        [Fact]
        public void Throws_within_When()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                Given(() =>
                {
                    Aggregate = new DummyAggregate("id");
                });

                When(() =>
                {
                    throw new Exception("Throws in given");
                });

                Dispose();
            });

            Mute();
            Assert.Equal("Throws in given", exception.Message);
        }

        [Fact]
        public void ThenThrows_capures_exception_from_When()
        {
            Given(() =>
            {
                Aggregate = new DummyAggregate("id");
            });

            When(() =>
            {
                throw new Exception("Throws in given");
            });

            ThenThrows<Exception>();
        }

        [Fact]
        public void ThenThrows_captures_exception_from_Given()
        {
            Given(() =>
            {
                Aggregate = new DummyAggregate("id");
                throw new Exception("Throws in given");
            });

            ThenThrows<Exception>();
        }

        [Fact]
        public void ThenThrows_with_message_captures_exception_from_Given()
        {
            Given(() =>
            {
                Aggregate = new DummyAggregate("id");
                throw new Exception("Throws in given");
            });

            ThenThrows<Exception>(ex => ex.Message == "Throws in given");
        }

        [Fact]
        public void ThenThrows_cant_be_used_multiple_times()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                Given(() =>
                {
                    Aggregate = new DummyAggregate("id");
                    throw new Exception("Throws in given");
                });

                ThenThrows<Exception>();
                ThenThrows<Exception>();
            });

            Assert.Equal("Exception has already been catched", exception.Message);
        }

        [Fact]
        public void Cant_mix_ThenThrows_with_Then()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                Given(() =>
                {
                    Aggregate = new DummyAggregate("id");
                    throw new Exception("Throws in given");
                });

                ThenThrows<Exception>();
                ThenNothing();
                Dispose();
            });

            Assert.Equal("Then has already been specified", exception.Message);
        }

        [Fact]
        public void Match_with_is_from_Given()
        {
            Given(() =>
            {
                Aggregate = new DummyAggregate("id");
            });

            Then(() =>
            {
                Is<DummyCreated>();
            });
        }

        [Fact]
        public void Match_with_is_from_When()
        {
            Given(() =>
            {
                Aggregate = new DummyAggregate("id");
            });

            When(() =>
            {
                Aggregate.Touch(1);
            });

            Then(() =>
            {
                Is<DummyTouch>();
            });
        }

        [Fact]
        public void Match_with_is_from_Given_with_param()
        {
            Given(() =>
            {
                Aggregate = new DummyAggregate("id");
            });

            Then(() =>
            {
                Is<DummyCreated>(p => p.Id == "id");
            });
        }

        [Fact]
        public void Match_with_is_from_When_with_param()
        {
            Given(() =>
            {
                Aggregate = new DummyAggregate("id");
            });

            When(() =>
            {
                Aggregate.Touch(1);
            });

            Then(() =>
            {
                Is<DummyTouch>(p => p.No == 1);
            });
        }

        [Fact]
        public void Invalid_match_with_is_from_Given()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                Given(() =>
                {
                    Aggregate = new DummyAggregate("id");
                });

                Then(() =>
                {
                    Is<DummyCreated>(p => p.Id == "idX");
                });

                Dispose();
            });
        }

        [Fact]
        public void Invalid_match_with_is_from_When()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                Given(() =>
                {
                    Aggregate = new DummyAggregate("id");
                });

                When(() =>
                {
                    Aggregate.Touch(1);
                });

                Then(() =>
                {
                    Is<DummyTouch>(p => p.No == 2);
                });

                Dispose();
            });
        }

        [Fact]
        public void ThenNothing_cant_match_emitted_events()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                Given(() =>
                {
                    Aggregate = new DummyAggregate("id");
                });

                ThenNothing();

                Dispose();
            });

			Assert.StartsWith("Did not match all events 0 vs. 1", exception.Message);
        }

        [Fact]
        public void ThenNothing_matches_no_events()
        {
            Given(() =>
            {
                Aggregate = new DummyAggregate("id");
            });

            When(() =>
            {
            });

            ThenNothing();

            Dispose();
        }
    }
}