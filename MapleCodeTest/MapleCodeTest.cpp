#include "pch.h"
#include <algorithm>

using namespace MapleCode::Reader;

namespace Microsoft::VisualStudio::CppUnitTestFramework
{
	template<> inline std::wstring ToString<Node>(const Node& t)
	{
		std::stringstream ss;
		ss << "{ " << t.GetNodeType()->GetName() << " @ " << t.GetOffset() << " }";

		std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
		auto u16str = convert.from_bytes(ss.str());
		return std::wstring((wchar_t*)u16str.c_str());
	}
}

namespace MapleCodeTest
{
	TEST_CLASS(MapleCodeTest)
	{
	public:
		TEST_METHOD(TestReadReference)
		{
			static const char data[] = {
				0x55, 0x03, 0x04, 0x08, 0x09,

				0x00, 0x05, 0x07,

				0x00, 0x02, 0x00, 0x00,

				0x00, 0x00, 0x04, 0x01,
				0x00, 0x00, 0x04, 0x02,

				0x6E, 0x00,
				0x02, 0x09, 0x0A,
				0x78, 0x00,
				0x79, 0x00,
			};
			auto doc = Document::ReadFromData(nullptr, data, sizeof(data));
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
