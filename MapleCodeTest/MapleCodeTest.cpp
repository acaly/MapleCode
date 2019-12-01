#include "pch.h"
#include "CppUnitTest.h"
#include "../MapleCode/MapleCodeReader.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace MapleCode::Reader;

namespace MapleCodeTest
{
	TEST_CLASS(MapleCodeTest)
	{
	public:
		
		TEST_METHOD(TestMethod1)
		{
			static const char data[] = {
				0x55,
				0x03, 0x04, 0x08, 0x09,

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
			auto nodes = doc->GetAllNodes();
		}
	};
}
