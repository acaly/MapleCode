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

	class ReaderException : public std::exception
	{
	public:
		ReaderException(const char* msg) : std::exception(msg) {}
	};

	enum class NodeArgumentType : std::uint8_t
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
		std::string _name;
		std::uint32_t _genericArgCount;
		std::vector<NodeArgumentType> _argumentTypes;
		bool _hasChildren;
		std::uint32_t _totalLen;

	public:
		NodeType(std::string name, std::uint8_t genericCount, std::vector<NodeArgumentType>&& args,
			bool hasChildren, std::uint32_t totalLen)
			: _name(std::move(name)), _genericArgCount(genericCount), _argumentTypes(std::move(args)),
			_hasChildren(hasChildren), _totalLen(totalLen)
		{
		}

		std::string GetName() { return _name; }
		std::uint32_t GetGenericArgCount() { return _genericArgCount; }

		const std::vector<NodeArgumentType>& GetArgumentTypes()
		{
			return _argumentTypes;
		}

		bool HasChildren() { return _hasChildren; }
		std::uint32_t GetTotalLen() { return _totalLen; }
	};

	struct Node
	{
	private:
		DocumentData* _document;
		std::uint32_t _offset;

	public:
		Node(DocumentData* doc, std::uint32_t offset)
			: _document(doc), _offset(offset)
		{
		}

		bool IsNull() const { return _document != nullptr; }
		std::uint32_t GetOffset() const { return _offset; }

		NodeType* GetNodeType() const;
		void ReadGenericArguments(std::vector<std::string>& results) const;
		void ReadArguments(std::vector<NodeArgument>& results) const;
		NodeRange GetChildren() const;
		Node FindParent() const;

		bool operator==(const Node& other) const
		{
			return _document == other._document && _offset == other._offset;
		}

		bool operator!=(const Node& other) const
		{
			return !(*this == other);
		}
	};

	struct NodeRange
	{
	private:
		DocumentData* _document;
		std::uint32_t _begin, _end;

	public:
		NodeRange(DocumentData* doc, std::uint32_t begin, std::uint32_t end)
			: _document(doc), _begin(begin), _end(end)
		{
		}

		struct NodeIterator
		{
			typedef std::input_iterator_tag iterator_category;
			typedef Node value_type;
			typedef std::ptrdiff_t difference_type;
			typedef Node* pointer;
			typedef Node& reference;

			DocumentData* _document;
			std::uint32_t _offset;
			Node _internalNode;

			bool operator==(const NodeIterator& other) const
			{
				return _document == other._document &&
					_offset == other._offset;
			}

			bool operator!=(const NodeIterator& other) const
			{
				return !(*this == other);
			}

			const Node& operator*() const
			{
				return _internalNode;
			}

			Node& operator*()
			{
				return _internalNode;
			}

			const Node* operator->() const
			{
				ValidateInternalNode();
				return &_internalNode;
			}

			Node* operator->()
			{
				ValidateInternalNode();
				return &_internalNode;
			}

			NodeIterator& operator++();

			NodeIterator operator++(int)
			{
				NodeIterator ret = *this;
				++(*this);
				return ret;
			}

		private:
			void ValidateInternalNode() const;
		};

		NodeIterator begin() { return { _document, _begin, { _document, _begin } }; }
		NodeIterator end() { return { _document, _end, { _document, _end } }; }

		std::vector<Node> ToList()
		{
			return { begin(), end() };
		}
	};

	struct NodeArgument
	{
	private:
		DocumentData* _document;
		NodeArgumentType _type;
		std::uint32_t _offset;

	public:
		NodeArgument(DocumentData* doc, NodeArgumentType type, std::uint32_t offset)
			: _document(doc), _type(type), _offset(offset)
		{
		}

		NodeArgumentType GetArgumentType() { return _type; }

		std::int32_t GetSigned();
		std::uint32_t GetUnsigned();
		std::string GetString();
		float GetFloat();

		template <typename T>
		void GetData(std::vector<T>& result)
		{
			std::uint32_t begin, end;
			GetDataRange(&begin, &end);
			auto len = end - begin;
			auto num = len / sizeof(T);
			if (num * sizeof(T) != len)
			{
				throw ReaderException("Incorrect data buffer type");
			}
			result.clear();
			result.resize(num);
			FillData(result.data(), begin, end);
		}

		Node GetNode();
		std::tuple<Node, std::string> GetField();

	private:
		std::uint32_t ReadArgNumber(int size);
		void GetDataRange(std::uint32_t* pBegin, std::uint32_t* pEnd);
		void FillData(void* buffer, std::uint32_t begin, std::uint32_t end);
	};

	struct DocumentData
	{
		struct TableRange
		{
			std::uint32_t Start = 0, End = 0;

			void Move(std::uint32_t num)
			{
				Start += num;
				End += num;
			}

			std::uint32_t GetLength() const
			{
				return End - Start;
			}
		};

		std::unique_ptr<std::uint8_t[]> Data;
		int StrWidth = 0, TypeWidth = 0, NodeWidth = 0, DataWidth = 0;

		std::vector<std::string> StrList;
		std::vector<NodeType> TypeList;

		TableRange StrRange, TypeRange, NodeRange, DataRange;

		std::vector<std::uint32_t> ArgumentWidth;
	};

	class Document
	{
	private:
		DocumentData Data;

	public:
		static std::unique_ptr<Document> ReadFromData(Document* typeList, const void* data, std::uint32_t length);

		NodeRange GetAllNodes()
		{
			return { &Data, 0, Data.NodeRange.End - Data.NodeRange.Start };
		}
	};
}
