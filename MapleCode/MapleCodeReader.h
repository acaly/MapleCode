#pragma once
#include <memory>
#include <vector>
#include <string>
#include <cstdint>

namespace MapleCode::Reader
{
	class Document;
	struct NodeArgument;
	struct DocumentData;
	struct NodeRange;

	enum NodeArgumentType : std::uint8_t
	{
		U8 = 0,
		U16 = 1,
		U32 = 2,
		S8 = 3,
		S16 = 4,
		S32 = 5,
		F32 = 6,
		STR = 7,
		DAT = 8,
		REF = 9,
		REFFIELD = 10,
	};

	class NodeType
	{
	private:
		std::string Name;
		std::uint32_t GenericArgCount;
		std::vector<NodeArgumentType> ArgumentTypes;
		bool Children;

	public:
		NodeType(std::string name, std::uint8_t genericCount, std::vector<NodeArgumentType>&& args,
			bool hasChildren)
			: Name(std::move(name)), GenericArgCount(genericCount), ArgumentTypes(std::move(args)),
				Children(hasChildren)
		{
		}

		std::string GetName() { return Name; }
		std::uint32_t GetGenericArgCount() { return GenericArgCount; }

		const std::vector<NodeArgumentType>& GetArgumentTypes()
		{
			return ArgumentTypes;
		}

		bool HasChildren() { return Children; }
	};

	struct Node
	{
	private:
		DocumentData* Document;
		std::int32_t Offset;

	public:
		Node(DocumentData* doc, std::int32_t offset)
			: Document(doc), Offset(offset)
		{
		}

		bool IsNull() { return Document != nullptr; }
		NodeType* GetNodeType();
		std::unique_ptr<std::string[]> ReadGenericArguments();
		std::unique_ptr<NodeArgument[]> ReadArguments();
		NodeRange GetChildren();
		Node FindParent();
	};

	struct NodeRange
	{
	private:
		DocumentData* Document;
		std::int32_t Begin, End;

	public:
		NodeRange(DocumentData* doc, std::int32_t begin, std::int32_t end)
			: Document(doc), Begin(begin), End(end)
		{
		}

		struct NodeIterator
		{
			DocumentData* Document;
			std::int32_t Offset;

			bool operator==(const NodeIterator& other)
			{
				return Document == other.Document &&
					Offset == other.Offset;
			}

			bool operator!=(const NodeIterator& other)
			{
				return !(*this == other);
			}

			Node operator*() const
			{
				return { Document, Offset };
			}

			Node operator->() const
			{
				return { Document, Offset };
			}
		};

		NodeIterator begin();
		NodeIterator end();
	};

	struct NodeArgument
	{
	private:
		DocumentData* Document;
		NodeArgumentType Type;
		std::uint32_t Offset;

	public:
		std::int32_t GetSigned();
		std::uint32_t GetUnsigned();
		std::string GetString();
		float GetFloat();
		std::unique_ptr<char[]> GetData();
		Node GetNode();
		std::string GetField();
	};

	struct DocumentData
	{
		struct TableRange
		{
			std::int32_t Start, End;

			void Move(std::int32_t num)
			{
				Start += num;
				End += num;
			}
		};
		static constexpr TableRange EmptyRange = { -1, -1 };

		std::unique_ptr<std::uint8_t[]> Data;
		int StrWidth, TypeWidth, NodeWidth, DataWidth;

		std::vector<std::string> StrList;
		std::vector<NodeType> TypeList;

		TableRange StrRange, TypeRange, NodeRange, DataRange;
	};

	class Document
	{
	private:
		DocumentData Data;

	public:
		static std::unique_ptr<Document> ReadFromData(Document* typeList, const void* data, int length);

		NodeRange GetAllNodes()
		{
			return { &Data, 0, Data.NodeRange.End - Data.NodeRange.Start };
		}
	};
}
