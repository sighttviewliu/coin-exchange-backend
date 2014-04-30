﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinExchange.Common.Domain.Model;
using CoinExchange.Trades.Domain.Model.OrderAggregate;
using CoinExchange.Trades.Domain.Model.OrderMatchingEngine;
using CoinExchange.Trades.Domain.Model.Services;
using CoinExchange.Trades.Infrastructure.Persistence.RavenDb;
using CoinExchange.Trades.Infrastructure.Services;
using CoinExchange.Trades.ReadModel.MemoryImages;
using Disruptor;
using NUnit.Framework;

namespace CoinExchange.Trades.ReadModel.IntegrationTests
{
    [TestFixture]
    class DepthMIIntegrationTests
    {
        private const string Integration = "Integration";

        #region Disruptor Linkage Tests

        [Test]
        [Category(Integration)]
        public void NewOrderDepthUpdatetest_ChecksWhetherDepthMemeoryImageGetsUpdatedWhenOrdersAreInserted_VerifiesThroughMemoryImagesLists()
        {
            DepthMemoryImage depthMemoryImage = new DepthMemoryImage();
            // Initialize the output Disruptor and assign the journaler as the event handler
            IEventStore eventStore = new RavenNEventStore();
            Journaler journaler = new Journaler(eventStore);
            OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });

            // Start exchagne to accept orders
            Exchange exchange = new Exchange();
            Order buyOrder1 = OrderFactory.CreateOrder("1233", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 491.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());

            // No matching orders till now
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            exchange.PlaceNewOrder(sellOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(5, exchange.OrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.OrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());
            
            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item2); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item1); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item3); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Item2);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[1].Item1);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[1].Item3);

            // Values at second depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item2);
            Assert.AreEqual(600, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item1);
            Assert.AreEqual(3, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item3);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item2);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item1);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item3);
        }

        [Test]
        [Category(Integration)]
        public void SellOrderMatctest_InCaseOfIncomingSellMatchChecksIfDepthGetsUpdatedAtMemoryImage_VerifiesThroughMemoryImagesLists()
        {
            DepthMemoryImage depthMemoryImage = new DepthMemoryImage();
            // Initialize the output Disruptor and assign the journaler as the event handler
            IEventStore eventStore = new RavenNEventStore();
            Journaler journaler = new Journaler(eventStore);
            OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });

            // Start exchagne to accept orders
            Exchange exchange = new Exchange();
            Order buyOrder1 = OrderFactory.CreateOrder("1233", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 491.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 700, 491.34M, new StubbedOrderIdGenerator());

            // No matching orders till now
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            
            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(5, exchange.OrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.OrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item2); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item1); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item3); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Item2);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[1].Item1);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[1].Item3);

            // Values at second depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item2);
            Assert.AreEqual(400, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item1);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item3);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item2);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item1);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item3);

            // Place a matching sell order to fill buy orders at 491.34
            exchange.PlaceNewOrder(sellOrder5);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(2, exchange.OrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.OrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.AskDepths.First().Key);

            // Only one bid level remains as the level at 491.34 got filled by the incoming sell match
            Assert.AreEqual(1, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item2); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item1); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item3); // Number of orders

            // Values at second depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item2);
            Assert.AreEqual(400, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item1);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item3);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item2);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item1);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item3);
        }

        [Test]
        [Category(Integration)]
        public void MultipleSellOrderMatctest_InCaseOfIncomingSellOrdersMatchEverytimeAndDepthUpdatedForBuyAtMemoryImage_VerifiesThroughMemoryImagesLists()
        {
            DepthMemoryImage depthMemoryImage = new DepthMemoryImage();
            // Initialize the output Disruptor and assign the journaler as the event handler
            IEventStore eventStore = new RavenNEventStore();
            Journaler journaler = new Journaler(eventStore);
            OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });

            // Start exchagne to accept orders
            Exchange exchange = new Exchange();
            Order buyOrder1 = OrderFactory.CreateOrder("1233", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 491.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 600, 491.34M, new StubbedOrderIdGenerator());
            Order sellOrder6 = OrderFactory.CreateOrder("1238998", CurrencyConstants.BitCoinUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 100, 493.34M, new StubbedOrderIdGenerator());

            // No matching orders till now
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(5, exchange.OrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.OrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item2); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item1); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item3); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Item2);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[1].Item1);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[1].Item3);

            // Values at second depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item2);
            Assert.AreEqual(400, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item1);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item3);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item2);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item1);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item3);

            // Place a matching sell order to fill buy orders at 491.34
            exchange.PlaceNewOrder(sellOrder5);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(2, exchange.OrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.OrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BitCoinUsd, depthMemoryImage.AskDepths.First().Key);

            // Only one bid level remains as the level at 491.34 got filled by the incoming sell match
            Assert.AreEqual(1, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Values at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item2); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item1); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item3); // Number of orders

            // Values at second depth level price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item2); // Price
            Assert.AreEqual(100, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item1); // Volume
            Assert.AreEqual(1, depthMemoryImage.BidDepths.First().Value.ToList()[0].Item3); // Number of orders

            // Values at first depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item2);
            Assert.AreEqual(400, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item1);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].Item3);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item2);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item1);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].Item3);
        }

        #endregion Disruptor Linkage Tests
    }
}