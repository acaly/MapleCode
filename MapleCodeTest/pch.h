#pragma once
#define _SILENCE_CXX17_CODECVT_HEADER_DEPRECATION_WARNING 1

#include "CppUnitTest.h"
#include "../MapleCode/MapleCodeReader.h"

#include <string>
#include <locale>
#include <codecvt>
#include <sstream>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace Microsoft::VisualStudio::CppUnitTestFramework
{
	using Node = MapleCode::Reader::Node;
	template<> inline std::wstring ToString<Node>(const Node& t)
	{
		std::stringstream ss;
		ss << "{ " << t.GetNodeType()->GetName() << " @ " << t.GetOffset() << " }";

		std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
		auto u16str = convert.from_bytes(ss.str());
		return std::wstring((wchar_t*)u16str.c_str());
	}

	template<> inline std::wstring ToString<std::vector<std::uint8_t>>(const std::vector<std::uint8_t>& t)
	{
		std::wstringstream ss;
		ss << "{ ";
		bool isFirst = true;
		for (auto i : t)
		{
			if (isFirst)
			{
				isFirst = false;
				ss << i;
			}
			else
			{
				ss << ", " << i;
			}
		}
		if (isFirst)
		{
			ss << "}";
		}
		else
		{
			ss << " }";
		}
		return ss.str();
	}
}
