#pragma once

#include <iostream>
#include <map>
#include <math.h>
#include <sstream>
#include <stdio.h>
#include <string>
#include <tuple>
#include <vector>
#include "Veque.h"

// All of the parsing code is from SiMBA++(https://github.com/pgarba/SiMBA-).
namespace Gamba {
    void report_fatal_error(const char* text, bool b = false) {
        printf(text);
        printf("/n");
        //throw std::invalid_argument(text);
    }


    class Token {
    public:
        enum class Type {
            Unknown,
            Number,
            Operator,
            LeftParen,
            RightParen,
            Variable
        };

        Token(Type type, const std::string& s, int precedence = -1,
            bool rightAssociative = false, bool unary = false)
            : type{ type }, str(s), precedence{ precedence },
            rightAssociative{ rightAssociative }, unary{ unary }, ArgIndex{ 0 } {}

        Type type;

        std::string str;

        int precedence;

        bool rightAssociative;

        bool unary;

        // Arg Index if Variable type
        int ArgIndex;
    };

    int8_t isVariable(const char* c, std::vector<std::string>* VNames) {
        if (VNames == nullptr)
            report_fatal_error("VNames is nullptr");

        for (int i = 0; i < VNames->size(); i++) {
            if (strncmp(c, (*VNames)[i].c_str(), (*VNames)[i].size()) == 0)
                return i;
        }

        return -1;
    }

    int countOperators(std::string& expr) {
        int OpCount = 0;
        for (const auto* p = expr.c_str(); *p; ++p) {
            switch (*p) {
            case '*':
                OpCount++;
                break;
            case '/':
                OpCount++;
                break;
            case '&':
                OpCount++;
                break;
            case '|':
                OpCount++;
                break;
            case '^':
                OpCount++;
                break;
            case '+':
                OpCount++;
                break;
            case '~':
                OpCount++;
                break;
            case '!':
                OpCount++;
                break;
            case '-':
                OpCount++;
                break;
            default:
                break;
            }
        }
        return OpCount;
    }

    void exprToTokens(const std::string& expr, veque::veque<Token>& tokens,
        bool detectVariables = false,
        std::vector<std::string>* VNames = nullptr) {
        int Index;
        for (const auto* p = expr.c_str(); *p; ++p) {
            if (isblank(*p)) {
                // do nothing
            }
            else if (detectVariables && (Index = isVariable(p, VNames)) != -1) {
                // detected a variable
                auto t = Token{ Token::Type::Variable, (*VNames)[Index] };
                t.ArgIndex = Index;
                tokens.push_back(t);

                // skip the variable name
                p += (*VNames)[Index].size() - 1;
            }
            else if (isdigit(*p)) {
                const auto* b = p;
                while (isdigit(*p)) {
                    ++p;
                }
                const auto s = std::string(b, p);
                tokens.push_back(Token{ Token::Type::Number, s });
                --p;
            }
            else {
                Token::Type t = Token::Type::Unknown;
                int precedence = -1;
                bool rightAssociative = false;
                bool unary = false;
                char c = *p;
                switch (c) {
                default:
                    std::cout << "[exprToTokens]: Unkown Token '" << c << "'\n";
                    report_fatal_error("", false);
                    break;
                case '(':
                    t = Token::Type::LeftParen;
                    break;
                case ')':
                    t = Token::Type::RightParen;
                    break;
                case '*':
                    t = Token::Type::Operator;
                    precedence = 7;
                    break;
                case '>':
                    t = Token::Type::Operator;
                    precedence = 5;
                    break;
                case '#':
                    t = Token::Type::Operator;
                    precedence = 8;
                    break;
                case '/':
                    t = Token::Type::Operator;
                    precedence = 7;
                    break;
                case '&':
                    t = Token::Type::Operator;
                    precedence = 4;
                    break;
                case '|':
                    t = Token::Type::Operator;
                    precedence = 2;
                    break;
                case '^':
                    t = Token::Type::Operator;
                    precedence = 3;
                    break;
                case '+':
                    t = Token::Type::Operator;
                    precedence = 6;
                    break;
                case '~':
                    unary = true;
                    t = Token::Type::Operator;
                    // Increase precendence if last token was unary and ~,! or -
                    if (!tokens.empty() && tokens.back().unary) {
                        // Calc this one first
                        precedence = tokens.back().precedence + 1;
                    }
                    else {
                        // Keep default
                        precedence = 9;
                    }
                    break;
                case '!':
                    unary = true;
                    t = Token::Type::Operator;
                    // Increase precendence if last token was unary and ~,! or -
                    if (!tokens.empty() && tokens.back().unary) {
                        // Calc this one first
                        precedence = tokens.back().precedence + 1;
                    }
                    else {
                        // Keep default
                        precedence = 9;
                    }
                    break;
                case '-':
                    // If current token is '-'
                    // and if it is the first token, or preceded by another operator, or
                    // left-paren,
                    if (tokens.empty() || tokens.back().type == Token::Type::Operator ||
                        tokens.back().type == Token::Type::LeftParen) {
                        // it's unary '-'
                        // note#1 : 'm' is a special operator name for unary '-'
                        // note#2 : It has highest precedence than any of the infix operators
                        unary = true;
                        c = 'm';
                        t = Token::Type::Operator;

                        // Increase precendence if last token was unary and ~,! or -
                        if (!tokens.empty() && tokens.back().unary) {
                            // Calc this one first
                            precedence = tokens.back().precedence + 1;
                        }
                        else {
                            // Keep default
                            precedence = 9;
                        }
                    }
                    else {
                        // otherwise, it's binary '-'
                        t = Token::Type::Operator;
                        precedence = 6;
                    }
                    break;
                }
                const auto s = std::string(1, c);
                tokens.push_back(Token{ t, s, precedence, rightAssociative, unary });
            }
        }
    }

    void shuntingYard(const veque::veque<Token>& tokens,
        veque::veque<Token>& queue) {
        veque::veque<Token> stack;

        // While there are tokens to be read:
        for (auto token : tokens) {
            // Read a token
            switch (token.type) {
            case Token::Type::Number:
                // If the token is a number, then add it to the output queue
                queue.push_back(token);
                break;

            case Token::Type::Operator: {
                // If the token is operator, o1, then:
                const auto o1 = token;

                // while there is an operator token,
                while (!stack.empty()) {
                    // o2, at the top of stack, and
                    const auto o2 = stack.back();

                    // either o1 is left-associative and its precedence is
                    // *less than or equal* to that of o2,
                    // or o1 if right associative, and has precedence
                    // *less than* that of o2,
                    if ((!o1.rightAssociative && o1.precedence <= o2.precedence) ||
                        (o1.rightAssociative && o1.precedence < o2.precedence)) {
                        // then pop o2 off the stack,
                        stack.pop_back();
                        // onto the output queue;
                        queue.push_back(o2);

                        continue;
                    }

                    // @@ otherwise, exit.
                    break;
                }

                // push o1 onto the stack.
                stack.push_back(o1);
            } break;

            case Token::Type::LeftParen:
                // If token is left parenthesis, then push it onto the stack
                stack.push_back(token);
                break;

            case Token::Type::RightParen:
                // If token is right parenthesis:
            {
                bool match = false;

                // Until the token at the top of the stack
                // is a left parenthesis,
                while (!stack.empty() && stack.back().type != Token::Type::LeftParen) {
                    // pop operators off the stack
                    // onto the output queue.
                    queue.push_back(stack.back());
                    stack.pop_back();
                    match = true;
                }

                if (!match && stack.empty()) {
                    // If the stack runs out without finding a left parenthesis,
                    // then there are mismatched parentheses.
                    printf("RightParen error (%s)\n", token.str.c_str());
                    return;
                }

                // Pop the left parenthesis from the stack,
                // but not onto the output queue.
                stack.pop_back();
            }
            break;
            case Token::Type::Variable: {
                queue.push_back(token);
            } break;

            default:
                printf("error (%s)\n", token.str.c_str());
                return;
            }
        }

        // When there are no more tokens to read:
        //   While there are still operator tokens in the stack:
        while (!stack.empty()) {
            // If the operator token on the top of the stack is a parenthesis,
            // then there are mismatched parentheses.
            if (stack.back().type == Token::Type::LeftParen) {
                printf("Mismatched parentheses error\n");
                return;
            }

            // Pop the operator onto the output queue.
            queue.push_back(std::move(stack.back()));
            stack.pop_back();
        }
    }

    void replace_all(std::string& str, const std::string& from,
        const std::string& to) {
        size_t start_pos = 0;
        while ((start_pos = str.find(from, start_pos)) != std::string::npos) {
            str.replace(start_pos, from.length(), to);
            start_pos += to.length();
        }
    }
}