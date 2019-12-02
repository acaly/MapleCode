#include "pch.h"
#include "TestFiles.h"

using namespace MapleCode::Reader;
using namespace MapleCodeTest::TestFiles;
using namespace std::string_literals;

namespace MapleCodeTest
{
	TEST_CLASS(MapleCodeTest)
	{
	public:
		TEST_METHOD(ReadSimpleNodes)
		{
			auto doc = Document::ReadFromData(nullptr, SimpleNodes.data(), SimpleNodes.size());
			auto nodes = doc->GetAllNodes().ToList();
			Assert::AreEqual(3u, nodes.size());

			std::vector<NodeArgument> args;

			auto n1 = nodes[0];
			Assert::AreEqual("node_a"s, n1.GetNodeType()->GetName());
			n1.ReadArguments(args);
			Assert::AreEqual(1u, args.size());
			Assert::AreEqual(10u, args[0].GetUnsigned());

			auto n2 = nodes[1];
			Assert::AreEqual("node_b"s, n2.GetNodeType()->GetName());
			n2.ReadArguments(args);
			Assert::AreEqual(3u, args.size());
			Assert::AreEqual(-1, args[0].GetSigned());
			Assert::AreEqual("string"s, args[1].GetString());
			Assert::AreEqual(0.1f, args[2].GetFloat());

			auto n3 = nodes[2];
			n3.ReadArguments(args);
			Assert::AreEqual("node_c"s, n3.GetNodeType()->GetName());
			std::vector<std::string> n3g;
			n3.ReadGenericArguments(n3g);
			Assert::AreEqual(2u, n3g.size());
			Assert::AreEqual("t1"s, n3g[0]);
			Assert::AreEqual("t2"s, n3g[1]);
			n3.ReadArguments(args);
			Assert::AreEqual(1u, args.size());
			std::vector<std::uint8_t> n3a3;
			args[0].GetData(n3a3);
			Assert::AreEqual(5u, n3a3.size());
			Assert::AreEqual(std::vector<std::uint8_t> { 0, 1, 2, 3, 4 }, n3a3);
		}

		TEST_METHOD(ReadChildren)
		{
			auto doc = Document::ReadFromData(nullptr, Children.data(), Children.size());
			auto nodes = doc->GetAllNodes().ToList();
			Assert::AreEqual(1u, nodes.size());
			auto n1 = nodes[0];

			Assert::AreEqual("node_a"s, n1.GetNodeType()->GetName());
			auto n1c = n1.GetChildren().ToList();
			Assert::AreEqual(2u, n1c.size());
			Assert::IsTrue(n1.FindParent().IsNull());

			auto n11 = n1c[0];
			Assert::AreEqual("node_b"s, n11.GetNodeType()->GetName());
			Assert::AreEqual(0u, n11.GetChildren().ToList().size());
			Assert::AreEqual(n1, n11.FindParent());

			auto n12 = n1c[1];
			Assert::AreEqual("node_a"s, n12.GetNodeType()->GetName());
			auto n12c = n12.GetChildren().ToList();
			Assert::AreEqual(2u, n12c.size());
			Assert::AreEqual(n1, n12.FindParent());

			auto n121 = n12c[0];
			Assert::AreEqual("node_a"s, n121.GetNodeType()->GetName());
			auto n121c = n121.GetChildren().ToList();
			Assert::AreEqual(1u, n121c.size());
			Assert::AreEqual(n12, n121.FindParent());

			auto n1211 = n121c[0];
			Assert::AreEqual("node_b"s, n1211.GetNodeType()->GetName());
			Assert::AreEqual(0u, n1211.GetChildren().ToList().size());
			Assert::AreEqual(n121, n1211.FindParent());

			auto n122 = n12c[1];
			Assert::AreEqual("node_b"s, n122.GetNodeType()->GetName());
			Assert::AreEqual(0u, n122.GetChildren().ToList().size());
			Assert::AreEqual(n12, n122.FindParent());
		}

		TEST_METHOD(ReadReference)
		{
			auto doc = Document::ReadFromData(nullptr, Reference.data(), Reference.size());
			auto nodes = doc->GetAllNodes().ToList();

			Assert::AreEqual(2u, nodes.size());
			auto n1 = nodes[0];
			auto n2 = nodes[1];
			std::vector<NodeArgument> n1c, n2c;
			n1.ReadArguments(n1c);
			n2.ReadArguments(n2c);

			Assert::AreEqual(n1, n1c[0].GetNode());
			Assert::AreEqual(n2, std::get<0>(n1c[1].GetField()));
			Assert::AreEqual(std::string("x"), std::get<1>(n1c[1].GetField()));

			Assert::AreEqual(n1, n2c[0].GetNode());
			Assert::AreEqual(n2, std::get<0>(n2c[1].GetField()));
			Assert::AreEqual(std::string("y"), std::get<1>(n2c[1].GetField()));
		}
	};
}
